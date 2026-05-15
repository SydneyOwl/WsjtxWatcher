using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Runtime.Versioning;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.ViewModels;
using WsjtxWatcher.UI;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/alert_rule_edit", Exported = false)]
public sealed class AlertRuleEditorActivity : LocalizedActivity
{
    public const string RuleIdExtra = "rule_id";
    public const string NewRuleTriggerTypeExtra = "new_rule_trigger_type";
    private const int NotificationPermissionRequestCode = 2102;

    private AlertRuleEditorViewModel _viewModel = null!;
    private INotificationService _notificationService = null!;
    private AlertRule _rule = null!;
    private bool _isBinding;
    private TextView _titleView = null!;
    private TextView _readOnlyHelp = null!;
    private TextView _ruleSourceValue = null!;
    private EditText _ruleNameValue = null!;
    private CheckBox _enabledCheckbox = null!;
    private TextView _ruleTriggerValue = null!;
    private EditText _rulePriorityValue = null!;
    private CheckBox _sendNotificationCheckbox = null!;
    private CheckBox _vibrationCheckbox = null!;
    private Button _openNotificationSettingsButton = null!;
    private EditText _cooldownValue = null!;
    private TextView _cooldownHelp = null!;
    private LinearLayout _conditionContainer = null!;
    private Button _deleteRuleButton = null!;
    private CheckBox? _pendingNotificationCheckbox;
    private Action<bool>? _pendingNotificationSetter;
    private bool _suppressNotificationToggleEvents;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_alert_rule_editor);

        _viewModel = AppHost.Current.GetRequiredService<AlertRuleEditorViewModel>();
        _notificationService = AppHost.Current.GetRequiredService<INotificationService>();
        BindViews();
        BindEvents();
        _ = LoadAsync();
    }

    protected override void OnPause()
    {
        if (!_isBinding && _rule is not null && !_rule.IsReadOnly)
        {
            _ = _viewModel.SaveAsync(_rule);
        }

        base.OnPause();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode != NotificationPermissionRequestCode)
        {
            return;
        }

        var granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
        if (granted)
        {
            _pendingNotificationSetter?.Invoke(true);
        }
        else if (_pendingNotificationCheckbox is not null)
        {
            _pendingNotificationSetter?.Invoke(false);
            SetNotificationCheckboxChecked(_pendingNotificationCheckbox, false);
            Toast.MakeText(this, GetString(Resource.String.denied_notification), ToastLength.Long)?.Show();
        }

        _pendingNotificationCheckbox = null;
        _pendingNotificationSetter = null;
        UpdateNotificationSettingsButtonVisibility();
    }

    private void BindViews()
    {
        _titleView = FindViewById<TextView>(Resource.Id.alert_rule_title)!;
        _readOnlyHelp = FindViewById<TextView>(Resource.Id.alert_rule_read_only_help)!;
        _ruleSourceValue = FindViewById<TextView>(Resource.Id.rule_source_value)!;
        _ruleNameValue = FindViewById<EditText>(Resource.Id.rule_name_value)!;
        _enabledCheckbox = FindViewById<CheckBox>(Resource.Id.alert_rule_enabled_checkbox)!;
        _ruleTriggerValue = FindViewById<TextView>(Resource.Id.rule_trigger_value)!;
        _rulePriorityValue = FindViewById<EditText>(Resource.Id.rule_priority_value)!;
        _sendNotificationCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_checkbox)!;
        _vibrationCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_checkbox)!;
        _openNotificationSettingsButton = FindViewById<Button>(Resource.Id.open_notification_settings)!;
        _cooldownValue = FindViewById<EditText>(Resource.Id.alert_rule_cooldown_value)!;
        _cooldownHelp = FindViewById<TextView>(Resource.Id.alert_rule_cooldown_help)!;
        _conditionContainer = FindViewById<LinearLayout>(Resource.Id.rule_condition_container)!;
        _deleteRuleButton = FindViewById<Button>(Resource.Id.delete_rule)!;
    }

    private void BindEvents()
    {
        _ruleNameValue.TextChanged += (_, _) =>
        {
            if (!_isBinding && !_rule.IsReadOnly)
            {
                _rule.Name = _ruleNameValue.Text ?? string.Empty;
                Title = RuleTextFormatter.GetRuleDisplayName(this, _rule);
                _titleView.Text = Title;
            }
        };

        _enabledCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding && !_rule.IsReadOnly)
            {
                _rule.IsEnabled = args.IsChecked;
            }
        };

        _rulePriorityValue.TextChanged += (_, _) =>
        {
            if (!_isBinding && !_rule.IsReadOnly && int.TryParse(_rulePriorityValue.Text, out var priority))
            {
                _rule.Priority = Math.Max(0, priority);
            }
        };

        _sendNotificationCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationCheckbox, value => _rule.Actions.SendNotification = value, args.IsChecked);

        _vibrationCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding && !_rule.IsReadOnly)
            {
                _rule.Actions.Vibrate = args.IsChecked;
            }
        };

        _cooldownValue.TextChanged += (_, _) =>
        {
            if (!_isBinding && !_rule.IsReadOnly && int.TryParse(_cooldownValue.Text, out var cooldownSeconds))
            {
                _rule.CooldownSeconds = Math.Max(0, cooldownSeconds);
                UpdateCooldownHelp();
            }
        };

        _openNotificationSettingsButton.Click += (_, _) => _notificationService.OpenNotificationSettings();
        _deleteRuleButton.Click += async (_, _) => await DeleteRuleAsync().ConfigureAwait(false);
    }

    private async Task LoadAsync()
    {
        _isBinding = true;
        var ruleId = Intent?.GetStringExtra(RuleIdExtra);
        if (!string.IsNullOrWhiteSpace(ruleId))
        {
            _rule = await _viewModel.LoadAsync(ruleId).ConfigureAwait(false);
        }
        else
        {
            var triggerTypeValue = Intent?.GetIntExtra(NewRuleTriggerTypeExtra, (int)RuleTriggerType.DecodeMessage) ?? (int)RuleTriggerType.DecodeMessage;
            var triggerType = Enum.IsDefined(typeof(RuleTriggerType), triggerTypeValue)
                ? (RuleTriggerType)triggerTypeValue
                : RuleTriggerType.DecodeMessage;
            _rule = await _viewModel.CreateAsync(triggerType).ConfigureAwait(false);
        }

        RunOnUiThread(() =>
        {
            BindRule();
            _isBinding = false;
        });
    }

    private void BindRule()
    {
        Title = RuleTextFormatter.GetRuleDisplayName(this, _rule);
        _titleView.Text = Title;
        _readOnlyHelp.Visibility = _rule.IsReadOnly ? ViewStates.Visible : ViewStates.Gone;
        _ruleSourceValue.Text = RuleTextFormatter.GetRuleSourceLabel(this, _rule.Source);
        _ruleNameValue.Text = _rule.IsReadOnly ? Title : _rule.Name;
        _enabledCheckbox.Checked = _rule.IsEnabled;
        _ruleTriggerValue.Text = RuleTextFormatter.GetTriggerLabel(this, _rule.TriggerType);
        _rulePriorityValue.Text = _rule.Priority.ToString();
        _sendNotificationCheckbox.Checked = _rule.Actions.SendNotification;
        _vibrationCheckbox.Checked = _rule.Actions.Vibrate;
        _cooldownValue.Text = _rule.CooldownSeconds.ToString();
        _deleteRuleButton.Visibility = !_rule.IsReadOnly ? ViewStates.Visible : ViewStates.Gone;

        _ruleNameValue.Enabled = !_rule.IsReadOnly;
        _enabledCheckbox.Enabled = !_rule.IsReadOnly;
        _rulePriorityValue.Enabled = !_rule.IsReadOnly;
        _sendNotificationCheckbox.Enabled = !_rule.IsReadOnly;
        _vibrationCheckbox.Enabled = !_rule.IsReadOnly;
        _cooldownValue.Enabled = !_rule.IsReadOnly;

        UpdateCooldownHelp();
        UpdateNotificationSettingsButtonVisibility();
        RenderConditions();
    }

    private void RenderConditions()
    {
        _conditionContainer.RemoveAllViews();
        _conditionContainer.AddView(CreateGroupView(_rule.RootCondition, null, isRoot: true));
    }

    private View CreateGroupView(RuleConditionGroup group, RuleConditionGroup? parentGroup, bool isRoot)
    {
        var card = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        card.SetBackgroundResource(Resource.Drawable.settings_card_background);
        card.SetPadding(Dp(12), Dp(12), Dp(12), Dp(12));
        card.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(10)
        };

        var modeSpinner = new Spinner(this);
        var modeLabels = new[]
        {
            RuleTextFormatter.GetGroupModeLabel(this, RuleConditionGroupMode.All),
            RuleTextFormatter.GetGroupModeLabel(this, RuleConditionGroupMode.Any)
        };
        var modeAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, modeLabels);
        modeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        modeSpinner.Adapter = modeAdapter;
        modeSpinner.SetSelection(group.Mode == RuleConditionGroupMode.Any ? 1 : 0);
        modeSpinner.Enabled = !_rule.IsReadOnly;
        modeSpinner.ItemSelected += (_, args) =>
        {
            if (_isBinding || _rule.IsReadOnly)
            {
                return;
            }

            group.Mode = args.Position == 1 ? RuleConditionGroupMode.Any : RuleConditionGroupMode.All;
        };

        card.AddView(modeSpinner);

        var childContainer = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        childContainer.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(8)
        };

        foreach (var child in group.Children)
        {
            switch (child)
            {
                case RuleConditionGroup childGroup:
                    childContainer.AddView(CreateGroupView(childGroup, group, isRoot: false));
                    break;
                case RulePredicate predicate:
                    childContainer.AddView(CreatePredicateView(predicate, group));
                    break;
                case RuleConstantPredicate constantPredicate:
                    childContainer.AddView(CreateConstantPredicateView(constantPredicate, group));
                    break;
            }
        }

        card.AddView(childContainer);

        if (!_rule.IsReadOnly)
        {
            var actionRow = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal
            };
            actionRow.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            {
                TopMargin = Dp(10)
            };

            var addPredicateButton = new Button(this)
            {
                Text = GetString(Resource.String.add_predicate)
            };
            addPredicateButton.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
            addPredicateButton.Click += (_, _) => ShowPredicateDialog(group, null);

            var addGroupButton = new Button(this)
            {
                Text = GetString(Resource.String.add_group)
            };
            addGroupButton.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f)
            {
                LeftMargin = Dp(8)
            };
            addGroupButton.Click += (_, _) =>
            {
                group.Children.Add(RuleConditionGroup.CreateAll());
                RenderConditions();
            };

            actionRow.AddView(addPredicateButton);
            actionRow.AddView(addGroupButton);
            card.AddView(actionRow);

            if (!isRoot && parentGroup is not null)
            {
                var deleteButton = new Button(this)
                {
                    Text = GetString(Resource.String.delete_group)
                };
                deleteButton.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
                {
                    TopMargin = Dp(8)
                };
                deleteButton.Click += (_, _) =>
                {
                    parentGroup.Children.Remove(group);
                    RenderConditions();
                };
                card.AddView(deleteButton);
            }
        }

        return card;
    }

    private View CreatePredicateView(RulePredicate predicate, RuleConditionGroup parentGroup)
    {
        var row = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        row.SetPadding(Dp(10), Dp(10), Dp(10), Dp(10));
        row.SetBackgroundColor(new Android.Graphics.Color(GetColor(Resource.Color.m3_surface_variant)));
        row.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(8)
        };

        var summary = new TextView(this)
        {
            Text = RuleTextFormatter.BuildPredicateSummary(this, predicate)
        };
        summary.SetTextColor(new Android.Graphics.Color(GetColor(Resource.Color.m3_on_surface)));
        row.AddView(summary);

        if (!_rule.IsReadOnly)
        {
            var actionRow = new LinearLayout(this)
            {
                Orientation = Orientation.Horizontal
            };
            actionRow.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            {
                TopMargin = Dp(8)
            };

            var editButton = new Button(this)
            {
                Text = GetString(Resource.String.edit_predicate)
            };
            editButton.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
            editButton.Click += (_, _) => ShowPredicateDialog(parentGroup, predicate);

            var deleteButton = new Button(this)
            {
                Text = GetString(Resource.String.delete_pattern)
            };
            deleteButton.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f)
            {
                LeftMargin = Dp(8)
            };
            deleteButton.Click += (_, _) =>
            {
                parentGroup.Children.Remove(predicate);
                RenderConditions();
            };

            actionRow.AddView(editButton);
            actionRow.AddView(deleteButton);
            row.AddView(actionRow);
        }

        return row;
    }

    private View CreateConstantPredicateView(RuleConstantPredicate predicate, RuleConditionGroup parentGroup)
    {
        var row = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        row.SetPadding(Dp(10), Dp(10), Dp(10), Dp(10));
        row.SetBackgroundColor(new Android.Graphics.Color(GetColor(Resource.Color.m3_surface_variant)));
        row.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(8)
        };

        var summary = new TextView(this)
        {
            Text = predicate.Value ? GetString(Resource.String.rule_constant_true) : GetString(Resource.String.rule_constant_false)
        };
        summary.SetTextColor(new Android.Graphics.Color(GetColor(Resource.Color.m3_on_surface)));
        row.AddView(summary);

        if (!_rule.IsReadOnly)
        {
            var deleteButton = new Button(this)
            {
                Text = GetString(Resource.String.delete_pattern)
            };
            deleteButton.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
            {
                TopMargin = Dp(8)
            };
            deleteButton.Click += (_, _) =>
            {
                parentGroup.Children.Remove(predicate);
                RenderConditions();
            };
            row.AddView(deleteButton);
        }

        return row;
    }

    private void ShowPredicateDialog(RuleConditionGroup parentGroup, RulePredicate? existingPredicate)
    {
        var triggerType = _rule.TriggerType;
        var fieldDefinitions = RuleMetadataCatalog.GetFieldDefinitions(triggerType).ToList();
        var workingPredicate = existingPredicate?.Clone() ?? CreateDefaultPredicate(triggerType);

        var layout = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        layout.SetPadding(Dp(16), Dp(8), Dp(16), Dp(8));

        var fieldLabel = CreateDialogLabel(Resource.String.predicate_field);
        var fieldSpinner = new Spinner(this);
        var fieldLabels = fieldDefinitions.Select(definition => RuleTextFormatter.GetFieldLabel(this, definition.Field)).ToArray();
        var fieldAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, fieldLabels);
        fieldAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        fieldSpinner.Adapter = fieldAdapter;

        var operatorLabel = CreateDialogLabel(Resource.String.predicate_operator);
        var operatorSpinner = new Spinner(this);

        var valueSourceLabel = CreateDialogLabel(Resource.String.predicate_value_source);
        var valueSourceSpinner = new Spinner(this);
        var valueSourceAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, new[]
        {
            GetString(Resource.String.predicate_literal_value),
            GetString(Resource.String.context_my_callsign),
            GetString(Resource.String.context_my_grid)
        });
        valueSourceAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        valueSourceSpinner.Adapter = valueSourceAdapter;

        var valueLabel = CreateDialogLabel(Resource.String.predicate_value);
        var valueEditText = new EditText(this);
        var boolSpinner = new Spinner(this);
        var boolAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, new[] { "true", "false" });
        boolAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        boolSpinner.Adapter = boolAdapter;

        var namedSetLabel = CreateDialogLabel(Resource.String.predicate_named_set);
        var namedSetSpinner = new Spinner(this);
        var namedSetAdapter = new ArrayAdapter<string>(
            this,
            Android.Resource.Layout.SimpleSpinnerItem,
            Enum.GetValues<RuleNamedSetRef>().Select(namedSet => RuleTextFormatter.GetNamedSetLabel(this, namedSet)).ToArray());
        namedSetAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        namedSetSpinner.Adapter = namedSetAdapter;

        var bandModeLabel = CreateDialogLabel(Resource.String.predicate_band_mode);
        var bandModeSpinner = new Spinner(this);
        var bandModeAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, new[]
        {
            GetString(Resource.String.named_set_match_band),
            GetString(Resource.String.named_set_ignore_band)
        });
        bandModeAdapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        bandModeSpinner.Adapter = bandModeAdapter;

        layout.AddView(fieldLabel);
        layout.AddView(fieldSpinner);
        layout.AddView(operatorLabel);
        layout.AddView(operatorSpinner);
        layout.AddView(valueSourceLabel);
        layout.AddView(valueSourceSpinner);
        layout.AddView(valueLabel);
        layout.AddView(valueEditText);
        layout.AddView(boolSpinner);
        layout.AddView(namedSetLabel);
        layout.AddView(namedSetSpinner);
        layout.AddView(bandModeLabel);
        layout.AddView(bandModeSpinner);

        var selectedFieldIndex = fieldDefinitions.FindIndex(definition => definition.Field == workingPredicate.Field);
        fieldSpinner.SetSelection(selectedFieldIndex >= 0 ? selectedFieldIndex : 0);

        void BindOperatorSpinner(RuleFieldDefinition definition)
        {
            var operatorLabels = definition.SupportedOperators.Select(@operator => RuleTextFormatter.GetOperatorLabel(this, @operator)).ToArray();
            var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, operatorLabels);
            adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
            operatorSpinner.Adapter = adapter;
            var operatorIndex = definition.SupportedOperators.ToList().IndexOf(workingPredicate.Operator);
            operatorSpinner.SetSelection(operatorIndex >= 0 ? operatorIndex : 0);
        }

        void UpdateValueViews()
        {
            var definition = fieldDefinitions[Math.Clamp(fieldSpinner.SelectedItemPosition, 0, fieldDefinitions.Count - 1)];
            var selectedOperator = definition.SupportedOperators[Math.Clamp(operatorSpinner.SelectedItemPosition, 0, definition.SupportedOperators.Count - 1)];
            var noValueOperator = selectedOperator is RuleOperator.IsTrue or RuleOperator.IsFalse or RuleOperator.Exists or RuleOperator.NotExists;
            var namedSetOperator = selectedOperator is RuleOperator.InNamedSet or RuleOperator.NotInNamedSet;
            var boolValue = definition.ValueType == RuleValueType.Boolean && !noValueOperator;
            var listValue = selectedOperator is RuleOperator.In or RuleOperator.NotIn;
            var contextEligible = definition.ValueType == RuleValueType.String && !namedSetOperator && !listValue && !boolValue && !noValueOperator;

            valueSourceLabel.Visibility = contextEligible ? ViewStates.Visible : ViewStates.Gone;
            valueSourceSpinner.Visibility = contextEligible ? ViewStates.Visible : ViewStates.Gone;
            valueLabel.Visibility = noValueOperator ? ViewStates.Gone : ViewStates.Visible;
            valueEditText.Visibility = (!namedSetOperator && !boolValue && !noValueOperator) ? ViewStates.Visible : ViewStates.Gone;
            boolSpinner.Visibility = boolValue ? ViewStates.Visible : ViewStates.Gone;
            namedSetLabel.Visibility = namedSetOperator ? ViewStates.Visible : ViewStates.Gone;
            namedSetSpinner.Visibility = namedSetOperator ? ViewStates.Visible : ViewStates.Gone;
            bandModeLabel.Visibility = namedSetOperator ? ViewStates.Visible : ViewStates.Gone;
            bandModeSpinner.Visibility = namedSetOperator ? ViewStates.Visible : ViewStates.Gone;
        }

        var initialFieldDefinition = fieldDefinitions[Math.Clamp(fieldSpinner.SelectedItemPosition, 0, fieldDefinitions.Count - 1)];
        BindOperatorSpinner(initialFieldDefinition);

        if (workingPredicate.Operand.Kind == RuleValueType.ContextRef)
        {
            valueSourceSpinner.SetSelection(workingPredicate.Operand.ContextRefValue == RuleContextRef.MyGrid ? 2 : 1);
        }
        else if (workingPredicate.Operand.Kind == RuleValueType.Boolean)
        {
            boolSpinner.SetSelection(workingPredicate.Operand.BooleanValue == false ? 1 : 0);
        }
        else if (workingPredicate.Operand.Kind == RuleValueType.NamedSetRef)
        {
            namedSetSpinner.SetSelection((int)(workingPredicate.Operand.NamedSetRefValue ?? RuleNamedSetRef.IgnoredCallsigns));
            bandModeSpinner.SetSelection(workingPredicate.Operand.NamedSetBandMatchModeValue == NamedSetBandMatchMode.IgnoreBand ? 1 : 0);
        }
        else if (workingPredicate.Operand.Kind == RuleValueType.Number)
        {
            valueEditText.Text = workingPredicate.Operand.NumberValue?.ToString() ?? string.Empty;
        }
        else if (workingPredicate.Operand.Kind == RuleValueType.NumberList)
        {
            valueEditText.Text = string.Join(",", workingPredicate.Operand.NumberListValue);
        }
        else if (workingPredicate.Operand.Kind == RuleValueType.StringList)
        {
            valueEditText.Text = string.Join(",", workingPredicate.Operand.StringListValue);
        }
        else
        {
            valueEditText.Text = workingPredicate.Operand.StringValue ?? string.Empty;
        }

        fieldSpinner.ItemSelected += (_, _) =>
        {
            var definition = fieldDefinitions[Math.Clamp(fieldSpinner.SelectedItemPosition, 0, fieldDefinitions.Count - 1)];
            workingPredicate.Field = definition.Field;
            if (!definition.SupportedOperators.Contains(workingPredicate.Operator))
            {
                workingPredicate.Operator = definition.SupportedOperators[0];
            }
            BindOperatorSpinner(definition);
            UpdateValueViews();
        };
        operatorSpinner.ItemSelected += (_, _) => UpdateValueViews();
        UpdateValueViews();

        var dialog = new AlertDialog.Builder(this)
            .SetTitle(existingPredicate is null ? Resource.String.add_predicate : Resource.String.edit_predicate)
            .SetView(layout)
            .SetPositiveButton(Android.Resource.String.Ok, (_, _) => { })
            .SetNegativeButton(Android.Resource.String.Cancel, (_, _) => { })
            .Create()!;
        dialog.Show();
        dialog.GetButton((int)DialogButtonType.Positive)?.SetOnClickListener(new ViewClickAction(_ =>
        {
            var definition = fieldDefinitions[Math.Clamp(fieldSpinner.SelectedItemPosition, 0, fieldDefinitions.Count - 1)];
            var selectedOperator = definition.SupportedOperators[Math.Clamp(operatorSpinner.SelectedItemPosition, 0, definition.SupportedOperators.Count - 1)];
            workingPredicate.Field = definition.Field;
            workingPredicate.Operator = selectedOperator;

            if (!TryBuildOperand(definition, selectedOperator, valueSourceSpinner.SelectedItemPosition, valueEditText.Text, boolSpinner.SelectedItemPosition, namedSetSpinner.SelectedItemPosition, bandModeSpinner.SelectedItemPosition, out var operand, out var errorResId))
            {
                Toast.MakeText(this, errorResId, ToastLength.Short)?.Show();
                return;
            }

            workingPredicate.Operand = operand;
            if (existingPredicate is null)
            {
                parentGroup.Children.Add(workingPredicate);
            }
            else
            {
                var index = parentGroup.Children.IndexOf(existingPredicate);
                if (index >= 0)
                {
                    parentGroup.Children[index] = workingPredicate;
                }
            }

            RenderConditions();
            dialog.Dismiss();
        }));
    }

    private bool TryBuildOperand(
        RuleFieldDefinition definition,
        RuleOperator selectedOperator,
        int valueSourceIndex,
        string? rawValue,
        int boolIndex,
        int namedSetIndex,
        int bandModeIndex,
        out RuleOperand operand,
        out int errorResId)
    {
        operand = new RuleOperand();
        errorResId = 0;

        if (selectedOperator is RuleOperator.IsTrue or RuleOperator.IsFalse or RuleOperator.Exists or RuleOperator.NotExists)
        {
            operand = new RuleOperand();
            return true;
        }

        if (selectedOperator is RuleOperator.InNamedSet or RuleOperator.NotInNamedSet)
        {
            operand = RuleOperand.ForNamedSet((RuleNamedSetRef)namedSetIndex, bandModeIndex == 1 ? NamedSetBandMatchMode.IgnoreBand : NamedSetBandMatchMode.MatchBand);
            return true;
        }

        if (definition.ValueType == RuleValueType.Boolean)
        {
            operand = RuleOperand.ForBoolean(boolIndex == 0);
            return true;
        }

        if (definition.ValueType == RuleValueType.Number)
        {
            if (selectedOperator is RuleOperator.In or RuleOperator.NotIn)
            {
                var numbers = (rawValue ?? string.Empty)
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Select(value => double.TryParse(value, out var number) ? (double?)number : null)
                    .ToList();
                if (numbers.Any(number => number is null))
                {
                    errorResId = Resource.String.invalid_rule_number;
                    return false;
                }

                operand = RuleOperand.ForNumberList(numbers.Select(number => number!.Value));
                return true;
            }

            if (!double.TryParse(rawValue, out var numberValue))
            {
                errorResId = Resource.String.invalid_rule_number;
                return false;
            }

            operand = RuleOperand.ForNumber(numberValue);
            return true;
        }

        if (valueSourceIndex == 1)
        {
            operand = RuleOperand.ForContextRef(RuleContextRef.MyCallsign);
            return true;
        }

        if (valueSourceIndex == 2)
        {
            operand = RuleOperand.ForContextRef(RuleContextRef.MyGrid);
            return true;
        }

        var textValue = (rawValue ?? string.Empty).Trim();
        if (selectedOperator == RuleOperator.Regex)
        {
            try
            {
                _ = System.Text.RegularExpressions.Regex.IsMatch(string.Empty, textValue);
            }
            catch (ArgumentException)
            {
                errorResId = Resource.String.invalid_rule_regex;
                return false;
            }
        }

        if (selectedOperator is RuleOperator.In or RuleOperator.NotIn)
        {
            operand = RuleOperand.ForStringList(textValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
            return true;
        }

        operand = RuleOperand.ForString(textValue);
        return true;
    }

    private RulePredicate CreateDefaultPredicate(RuleTriggerType triggerType)
    {
        var definition = RuleMetadataCatalog.GetFieldDefinitions(triggerType).First();
        var @operator = definition.SupportedOperators.First();
        return new RulePredicate
        {
            Field = definition.Field,
            Operator = @operator,
            Operand = CreateDefaultOperand(definition, @operator)
        };
    }

    private RuleOperand CreateDefaultOperand(RuleFieldDefinition definition, RuleOperator @operator)
    {
        if (@operator is RuleOperator.InNamedSet or RuleOperator.NotInNamedSet)
        {
            return RuleOperand.ForNamedSet(RuleNamedSetRef.IgnoredCallsigns);
        }

        if (@operator is RuleOperator.IsTrue or RuleOperator.IsFalse or RuleOperator.Exists or RuleOperator.NotExists)
        {
            return new RuleOperand();
        }

        return definition.ValueType switch
        {
            RuleValueType.Number => RuleOperand.ForNumber(0),
            RuleValueType.Boolean => RuleOperand.ForBoolean(true),
            _ => RuleOperand.ForString(string.Empty)
        };
    }

    private async Task DeleteRuleAsync()
    {
        if (_rule.IsReadOnly)
        {
            return;
        }

        var confirmed = await ConfirmAsync(Resource.String.delete_rule, Resource.String.delete_rule_confirm).ConfigureAwait(false);
        if (!confirmed)
        {
            return;
        }

        await _viewModel.DeleteAsync(_rule.Id).ConfigureAwait(false);
        RunOnUiThread(Finish);
    }

    private Task<bool> ConfirmAsync(int titleResId, int messageResId)
    {
        var tcs = new TaskCompletionSource<bool>();
        RunOnUiThread(() =>
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(titleResId);
            builder.SetMessage(messageResId);
            builder.SetPositiveButton(Android.Resource.String.Ok, (_, _) => tcs.TrySetResult(true));
            builder.SetNegativeButton(Android.Resource.String.Cancel, (_, _) => tcs.TrySetResult(false));
            var dialog = builder.Create() ?? throw new InvalidOperationException("Failed to create confirmation dialog.");
            dialog.CancelEvent += (_, _) => tcs.TrySetResult(false);
            dialog.Show();
        });
        return tcs.Task;
    }

    private TextView CreateDialogLabel(int textResId)
    {
        var textView = new TextView(this)
        {
            Text = GetString(textResId)
        };
        textView.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(10)
        };
        return textView;
    }

    private void UpdateCooldownHelp()
    {
        _cooldownHelp.Text = string.Format(GetString(Resource.String.alert_rule_cooldown_help), Math.Max(0, _rule.CooldownSeconds));
    }

    private void HandleNotificationToggle(CheckBox checkBox, Action<bool> setter, bool isChecked)
    {
        if (_isBinding || _suppressNotificationToggleEvents || _rule.IsReadOnly)
        {
            return;
        }

        if (!isChecked)
        {
            setter(false);
            UpdateNotificationSettingsButtonVisibility();
            return;
        }

        if (!RequiresNotificationPermissionRequest())
        {
            setter(true);
            UpdateNotificationSettingsButtonVisibility();
            return;
        }

        _pendingNotificationCheckbox = checkBox;
        _pendingNotificationSetter = setter;
        RequestNotificationPermission();
    }

    [SupportedOSPlatformGuard("android33.0")]
    private bool RequiresNotificationPermissionRequest()
    {
        return OperatingSystem.IsAndroidVersionAtLeast(33)
               && CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted;
    }

    [SupportedOSPlatform("android33.0")]
    private void RequestNotificationPermission()
    {
        RequestPermissions([Manifest.Permission.PostNotifications], NotificationPermissionRequestCode);
    }

    private void SetNotificationCheckboxChecked(CheckBox checkBox, bool isChecked)
    {
        _suppressNotificationToggleEvents = true;
        checkBox.Checked = isChecked;
        _suppressNotificationToggleEvents = false;
    }

    private void UpdateNotificationSettingsButtonVisibility()
    {
        _openNotificationSettingsButton.Visibility = _notificationService.AreNotificationsEnabled()
            ? ViewStates.Gone
            : ViewStates.Visible;
    }

    private int Dp(int value)
    {
        return (int)(value * (Resources?.DisplayMetrics?.Density ?? 1f));
    }

    private sealed class ViewClickAction : Java.Lang.Object, View.IOnClickListener
    {
        private readonly Action<View> _handler;

        public ViewClickAction(Action<View> handler)
        {
            _handler = handler;
        }

        public void OnClick(View? v)
        {
            if (v is not null)
            {
                _handler(v);
            }
        }
    }
}

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.ViewModels;
using WsjtxWatcher.UI;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/alert_rules", Exported = false)]
public sealed class AlertRulesActivity : LocalizedActivity
{
    private AlertRulesViewModel _viewModel = null!;
    private INotificationService _notificationService = null!;
    private LinearLayout _systemRuleList = null!;
    private LinearLayout _customRuleList = null!;
    private TextView _emptyCustomRules = null!;
    private Button _newCustomRuleButton = null!;
    private Button _openNotificationSettingsButton = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_alert_rules);

        _viewModel = AppHost.Current.GetRequiredService<AlertRulesViewModel>();
        _notificationService = AppHost.Current.GetRequiredService<INotificationService>();
        _systemRuleList = FindViewById<LinearLayout>(Resource.Id.system_rule_list)!;
        _customRuleList = FindViewById<LinearLayout>(Resource.Id.custom_rule_list)!;
        _emptyCustomRules = FindViewById<TextView>(Resource.Id.empty_custom_rules)!;
        _newCustomRuleButton = FindViewById<Button>(Resource.Id.new_custom_rule)!;
        _openNotificationSettingsButton = FindViewById<Button>(Resource.Id.open_notification_settings)!;

        _openNotificationSettingsButton.Click += (_, _) => _notificationService.OpenNotificationSettings();
        _newCustomRuleButton.Click += (_, _) => ShowCreateRuleDialog();
        _ = LoadAsync();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var rules = await _viewModel.LoadAsync().ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            RenderRules(rules);
            _openNotificationSettingsButton.Visibility = _notificationService.AreNotificationsEnabled()
                ? ViewStates.Gone
                : ViewStates.Visible;
        });
    }

    private void RenderRules(IReadOnlyList<AlertRule> rules)
    {
        _systemRuleList.RemoveAllViews();
        _customRuleList.RemoveAllViews();

        var systemRules = rules.Where(rule => rule.Source == RuleSource.SystemPreset).ToList();
        var customRules = rules.Where(rule => rule.Source == RuleSource.UserDefined).ToList();

        foreach (var rule in systemRules)
        {
            _systemRuleList.AddView(CreateRuleCard(rule));
        }

        foreach (var rule in customRules)
        {
            _customRuleList.AddView(CreateRuleCard(rule));
        }

        _emptyCustomRules.Visibility = customRules.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
    }

    private View CreateRuleCard(AlertRule rule)
    {
        var card = new LinearLayout(this)
        {
            Orientation = Orientation.Vertical
        };
        card.SetBackgroundResource(Resource.Drawable.settings_card_background);
        card.SetPadding(Dp(16), Dp(16), Dp(16), Dp(16));
        card.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(12)
        };

        var title = new TextView(this)
        {
            Text = RuleTextFormatter.GetRuleDisplayName(this, rule)
        };
        title.SetTextColor(new Android.Graphics.Color(GetColor(Resource.Color.m3_on_surface)));
        title.SetTextSize(Android.Util.ComplexUnitType.Sp, 16f);
        title.SetTypeface(title.Typeface, Android.Graphics.TypefaceStyle.Bold);

        var subtitle = new TextView(this)
        {
            Text = RuleTextFormatter.BuildRuleSummary(this, rule)
        };
        subtitle.SetTextColor(new Android.Graphics.Color(GetColor(Resource.Color.m3_on_surface_variant)));
        subtitle.SetTextSize(Android.Util.ComplexUnitType.Sp, 13f);
        subtitle.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(8)
        };

        var source = new TextView(this)
        {
            Text = RuleTextFormatter.GetRuleSourceLabel(this, rule.Source)
        };
        source.SetTextColor(new Android.Graphics.Color(GetColor(Resource.Color.m3_primary)));
        source.SetTextSize(Android.Util.ComplexUnitType.Sp, 12f);
        source.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(6)
        };

        var button = new Button(this)
        {
            Text = GetString(Resource.String.alert_rule_edit)
        };
        button.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(12)
        };
        button.Click += (_, _) =>
        {
            var intent = new Intent(this, typeof(AlertRuleEditorActivity));
            intent.PutExtra(AlertRuleEditorActivity.RuleIdExtra, rule.Id);
            StartActivity(intent);
        };

        card.AddView(title);
        card.AddView(source);
        card.AddView(subtitle);
        card.AddView(button);
        return card;
    }

    private void ShowCreateRuleDialog()
    {
        var items = new[]
        {
            GetString(Resource.String.create_rule_decode),
            GetString(Resource.String.create_rule_logged_qso)
        };
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.create_rule_title);
        builder.SetItems(items, (_, args) =>
        {
            var triggerType = args.Which == 1 ? RuleTriggerType.LoggedQso : RuleTriggerType.DecodeMessage;
            var intent = new Intent(this, typeof(AlertRuleEditorActivity));
            intent.PutExtra(AlertRuleEditorActivity.NewRuleTriggerTypeExtra, (int)triggerType);
            StartActivity(intent);
        });
        builder.SetNegativeButton(Android.Resource.String.Cancel, (_, _) => { });
        builder.Show();
    }

    private int Dp(int value)
    {
        return (int)(value * (Resources?.DisplayMetrics?.Density ?? 1f));
    }
}

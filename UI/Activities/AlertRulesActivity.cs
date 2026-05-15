using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.ViewModels;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/alert_rules", Exported = false)]
public sealed class AlertRulesActivity : LocalizedActivity
{
    private AlertRulesViewModel _viewModel = null!;
    private INotificationService _notificationService = null!;
    private LinearLayout _ruleList = null!;
    private Button _openNotificationSettingsButton = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_alert_rules);

        _viewModel = AppHost.Current.GetRequiredService<AlertRulesViewModel>();
        _notificationService = AppHost.Current.GetRequiredService<INotificationService>();
        _ruleList = FindViewById<LinearLayout>(Resource.Id.alert_rule_list)!;
        _openNotificationSettingsButton = FindViewById<Button>(Resource.Id.open_notification_settings)!;
        _openNotificationSettingsButton.Click += (_, _) =>
            _notificationService.OpenNotificationSettings();
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
        _ruleList.RemoveAllViews();
        foreach (var rule in rules)
        {
            _ruleList.AddView(CreateRuleCard(rule));
        }
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
            TopMargin = Dp(14)
        };

        var title = new TextView(this)
        {
            Text = GetRuleTitle(rule.Kind)
        };
        title.SetTextColor(new Android.Graphics.Color(AndroidX.Core.Content.ContextCompat.GetColor(this, Resource.Color.m3_on_surface)));
        title.SetTextSize(Android.Util.ComplexUnitType.Sp, 16f);
        title.SetTypeface(title.Typeface, Android.Graphics.TypefaceStyle.Bold);

        var subtitle = new TextView(this)
        {
            Text = BuildSummary(rule)
        };
        subtitle.SetTextColor(new Android.Graphics.Color(AndroidX.Core.Content.ContextCompat.GetColor(this, Resource.Color.m3_on_surface_variant)));
        subtitle.SetTextSize(Android.Util.ComplexUnitType.Sp, 13f);
        subtitle.LayoutParameters = new LinearLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(8)
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
        card.AddView(subtitle);
        card.AddView(button);
        return card;
    }

    private string BuildSummary(AlertRule rule)
    {
        var actions = rule.SendNotification && rule.Vibrate
            ? GetString(Resource.String.alert_rule_actions_notify_and_vibration)
            : rule.SendNotification
                ? GetString(Resource.String.alert_rule_actions_notify_only)
                : rule.Vibrate
                    ? GetString(Resource.String.alert_rule_actions_vibration_only)
                    : GetString(Resource.String.alert_rule_actions_none);

        var extra = rule.Kind switch
        {
            AlertRuleKind.WatchedCallsign => string.Format(
                GetString(Resource.String.alert_rule_summary_patterns),
                rule.CallsignPatterns.Count == 0 ? GetString(Resource.String.alert_rule_default_callsign_patterns) : rule.CallsignPatterns.Count.ToString()),
            AlertRuleKind.SelectedDxcc => string.Format(
                GetString(Resource.String.alert_rule_summary_dxcc),
                rule.SelectedDxccIds.Count),
            _ => string.Empty
        };

        var target = rule.Kind is AlertRuleKind.WatchedCallsign or AlertRuleKind.SelectedDxcc
            ? string.Format(GetString(Resource.String.alert_rule_summary_target), GetMatchTargetLabel(rule.MatchTarget))
            : string.Empty;

        return string.Join(" · ", new[] { actions, extra, target, string.Format(GetString(Resource.String.alert_rule_summary_cooldown), rule.CooldownSeconds) }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private string GetRuleTitle(AlertRuleKind kind)
    {
        return kind switch
        {
            AlertRuleKind.WatchedCallsign => GetString(Resource.String.when_callsign_included),
            AlertRuleKind.AnyMessage => GetString(Resource.String.when_call_all),
            AlertRuleKind.SelectedDxcc => GetString(Resource.String.on_dxcc),
            _ => GetString(Resource.String.on_logged_qso)
        };
    }

    private string GetMatchTargetLabel(AlertRuleMatchTarget matchTarget)
    {
        return matchTarget switch
        {
            AlertRuleMatchTarget.ReceiverOnly => GetString(Resource.String.selected_dxcc_match_target_receiver),
            AlertRuleMatchTarget.ReceiverOrTransmitter => GetString(Resource.String.selected_dxcc_match_target_both),
            _ => GetString(Resource.String.selected_dxcc_match_target_transmitter)
        };
    }

    private int Dp(int value)
    {
        return (int)(value * (Resources?.DisplayMetrics?.Density ?? 1f));
    }
}

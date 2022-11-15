import {
  Component, OnDestroy,
  OnInit
} from '@angular/core';
import {AuthStateService} from 'src/app/common/store';
import {Subscription} from 'rxjs';
import UserbackWidgetLoader, {UserbackWidget} from '@userback/widget';
import {AppSettingsStateService, AppSettingsQuery} from 'src/app/modules/application-settings/components/store';
import {UserbackWidgetSettingModel} from 'src/app/common/models';

@Component({
  selector: 'app-userback-widget',
  templateUrl: './userback-widget.component.html',
  styleUrls: ['./userback-widget.component.scss'],
})
export class UserbackWidgetComponent implements OnInit, OnDestroy {
  private isShowing: boolean = false;
  private loadWidget: boolean = false;
  private userbackToken: string;
  private widget: UserbackWidget;

  isAuthSub$: Subscription;
  getUserbackWidgetIsEnabledSub$: Subscription;

  constructor(
    private authStateService: AuthStateService,
    private appSettingsQuery: AppSettingsQuery,
    private appSettingsStateService: AppSettingsStateService,
  ) {
  }

  ngOnInit(): void {
    this.isAuthSub$ = this.authStateService.isAuthAsync.subscribe((isAuth?: boolean) => this.onIsAuthAsync(isAuth));
  }

  onIsAuthAsync(isAuth?: boolean) {
    if (isAuth && !this.getUserbackWidgetIsEnabledSub$) {
      this.appSettingsStateService.getOtherSettings();
      this.getUserbackWidgetIsEnabledSub$ = this.appSettingsQuery.selectOthersSettings$
        .subscribe((WidgetSettings) => WidgetSettings ? this.onSelectOthersSettings(WidgetSettings, isAuth) : void 0);
    }
  }

  onSelectOthersSettings(WidgetSettings: UserbackWidgetSettingModel, isAuth: boolean) {
    if (WidgetSettings.isUserbackWidgetEnabled) {
      this.userbackToken = WidgetSettings.userbackToken;
      if (isAuth && !this.isShowing && !this.loadWidget) {
        this.show();
      } else if (!isAuth && this.isShowing) {
        this.hide();
      }
    }
    if (!WidgetSettings.isUserbackWidgetEnabled && this.isShowing) {
      this.hide();
    }
  }


  hide(): void {
    this.widget.destroy();
    this.isShowing = false;
  }

  show(): void {
    this.loadWidget = true;
    UserbackWidgetLoader(this.userbackToken, {
      on_load: () => {
        // todo on_load don't work if javascript api not enabled. Detail https://support.userback.io/en/articles/5209252-javascript-api
        this.isShowing = true;
        this.loadWidget = false;
      },
      email: this.authStateService.currentUserName,
      name: this.authStateService.currentUserFullName,
    }).then(x => {
      // todo Promise don't work (not return widget) if javascript api not enabled. Detail https://support.userback.io/en/articles/5209252-javascript-api
      this.widget = x;
    });
  }

  ngOnDestroy(): void {
    this.hide();
    this.isAuthSub$.unsubscribe();
    this.getUserbackWidgetIsEnabledSub$.unsubscribe();
  }
}

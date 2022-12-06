import {Injectable} from '@angular/core';
import {AppSettingsService} from 'src/app/common/services';
import {AppSettingsQuery, AppSettingsStore} from './';
import {map} from 'rxjs/operators';
import {AdminSettingsModel} from 'src/app/common/models';
import {take} from 'rxjs';

@Injectable({providedIn: 'root'})
export class AppSettingsStateService {
  constructor(
    private store: AppSettingsStore,
    private service: AppSettingsService,
    private query: AppSettingsQuery
  ) {
    this.getAllAppSettings();
  }

  getAdminSettings() {
    return this.service.getAdminSettings().pipe(take(1));
  }

  getOtherSettings() {
    return this.service.getUserbackWidgetIsEnabled().pipe(take(1));
  }

  getAllAppSettings() {
    this.getAdminSettings().subscribe((response) => {
      if (response && response.success && response.model) {
        this.store.update(() => ({
          adminSettingsModel: response.model,
        }));
      }
      return response;
    });
    this.getOtherSettings().subscribe((response) => {
      if (response && response.success && response.model) {
        this.store.update((state) => ({
          othersSettings: {...state.othersSettings, ...response.model},
        }));
      }
    });
  }

  updateAdminSettings(adminSettings: AdminSettingsModel) {
    return this.service.updateAdminSettings(adminSettings)
      .pipe(
        map((response) => {
          if (response && response.success) {
            this.store.update(() => ({
              adminSettingsModel: adminSettings,
            }));
          }
          return response;
        })
      );
  }

  updateUserbackWidgetIsEnabled(UserbackWidgetIsEnabled: boolean) {
    return this.service.updateUserbackWidgetIsEnabled(UserbackWidgetIsEnabled)
      .pipe(
        map((response) => {
          if (response && response.success) {
            this.store.update((state) => ({
              othersSettings: {...state.othersSettings, isUserbackWidgetEnabled: UserbackWidgetIsEnabled},
            }));
          }
          return response;
        })
      );
  }
}

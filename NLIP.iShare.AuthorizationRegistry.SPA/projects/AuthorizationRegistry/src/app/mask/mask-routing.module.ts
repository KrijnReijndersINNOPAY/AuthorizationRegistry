import { Routes, RouterModule } from '@angular/router';
import { NgModule } from '@angular/core';
import { constants, RoleGuard } from 'common';
import { DelegationMaskComponent } from './components/delegation-mask/delegation-mask.component';

const routes: Routes = [
  {
    path: '',
    component: DelegationMaskComponent,
    canActivate: [RoleGuard],
    data: {
      allowedRoles: [constants.roles.EntitledPartyCreator, constants.roles.EntitledPartyViewer, constants.roles.SchemeOwner]
    }
  }
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MaskRoutingModule {}

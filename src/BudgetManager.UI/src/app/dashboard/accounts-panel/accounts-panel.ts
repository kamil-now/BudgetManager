import { Component, inject, input, output } from '@angular/core';
import { TranslatePipe } from '../../i18n/t.pipe';
import { AccountDTO } from '../../shared/models';
import { balanceLines } from '../../shared/money';
import { UiActions } from '../../shared/ui-actions';
import { Scope, isScope } from '../dashboard-data';

@Component({
  selector: 'app-accounts-panel',
  imports: [TranslatePipe],
  templateUrl: './accounts-panel.html',
  styleUrl: './accounts-panel.scss',
})
export class AccountsPanel {
  private readonly ui = inject(UiActions);

  readonly accounts = input.required<AccountDTO[]>();
  readonly scope = input<Scope | null>(null);
  readonly scopeChange = output<Scope | null>();

  protected readonly balanceLines = balanceLines;

  protected isScoped(account: AccountDTO): boolean {
    return isScope(this.scope(), 'account', account.id);
  }

  protected toggle(account: AccountDTO): void {
    this.scopeChange.emit(
      this.isScoped(account) ? null : { kind: 'account', id: account.id, name: account.name },
    );
  }

  protected addAccount(): void {
    this.ui.open({ kind: 'account' });
  }

  protected edit(account: AccountDTO): void {
    this.ui.open({ kind: 'account', account });
  }
}

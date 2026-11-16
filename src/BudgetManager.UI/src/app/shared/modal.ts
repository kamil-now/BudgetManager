import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '../i18n/t.pipe';
import { DialogDirective } from './dialog';

let modalSeq = 0;

export type ModalSize = 'sm' | 'md' | 'lg' | 'xl';

@Component({
  selector: 'app-modal',
  imports: [DialogDirective, TranslatePipe],
  templateUrl: './modal.html',
  host: { '(document:keydown.escape)': 'requestClose()' },
})
export class Modal {
  readonly title = input('');
  readonly submitLabel = input('');
  readonly busyLabel = input('');
  readonly submitting = input(false);
  readonly error = input('');
  readonly size = input<ModalSize>('md');
  readonly danger = input(false);
  readonly closed = output<void>();
  readonly submitted = output<void>();

  protected readonly titleId = `modal-title-${++modalSeq}`;

  protected requestClose(): void {
    if (!this.submitting()) this.closed.emit();
  }

  protected onBackdrop(event: MouseEvent): void {
    if (event.target === event.currentTarget) this.requestClose();
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.submitted.emit();
  }
}

import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-spinner',
  standalone: true,
  templateUrl: './spinner.component.html'
})
export class SpinnerComponent {
  @Input() label = 'Loading…';
}

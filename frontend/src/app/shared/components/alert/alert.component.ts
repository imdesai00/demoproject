import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-alert',
  standalone: true,
  templateUrl: './alert.component.html'
})
export class AlertComponent {
  @Input() message = '';
}

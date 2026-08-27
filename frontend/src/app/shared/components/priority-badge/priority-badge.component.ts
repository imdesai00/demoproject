import { NgClass } from '@angular/common';
import { Component, Input, computed, signal } from '@angular/core';
import { TaskPriority } from '../../models/task.model';

const PRIORITY_STYLES: Record<TaskPriority, { label: string; classes: string }> = {
  Low: { label: 'Low', classes: 'bg-surface-100 text-surface-600 border border-surface-200' },
  Medium: { label: 'Medium', classes: 'bg-brand-50 text-brand-700 border border-brand-200' },
  High: { label: 'High', classes: 'bg-rose-50 text-rose-700 border border-rose-200' }
};

@Component({
  selector: 'app-priority-badge',
  standalone: true,
  imports: [NgClass],
  templateUrl: './priority-badge.component.html'
})
export class PriorityBadgeComponent {
  private readonly prioritySignal = signal<TaskPriority>('Medium');

  @Input({ required: true })
  set priority(value: TaskPriority) {
    this.prioritySignal.set(value);
  }

  readonly style = computed(() => PRIORITY_STYLES[this.prioritySignal()]);
}

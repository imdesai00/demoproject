import { NgClass } from '@angular/common';
import { Component, Input, computed, signal } from '@angular/core';
import { ProjectTaskStatus } from '../../models/task.model';

const STATUS_STYLES: Record<ProjectTaskStatus, { label: string; classes: string }> = {
  Todo: { label: 'To Do', classes: 'bg-surface-200 text-surface-700' },
  InProgress: { label: 'In Progress', classes: 'bg-amber-100 text-amber-700' },
  Done: { label: 'Done', classes: 'bg-emerald-100 text-emerald-700' }
};

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [NgClass],
  templateUrl: './status-badge.component.html'
})
export class StatusBadgeComponent {
  private readonly statusSignal = signal<ProjectTaskStatus>('Todo');

  @Input({ required: true })
  set status(value: ProjectTaskStatus) {
    this.statusSignal.set(value);
  }

  readonly style = computed(() => STATUS_STYLES[this.statusSignal()]);
}

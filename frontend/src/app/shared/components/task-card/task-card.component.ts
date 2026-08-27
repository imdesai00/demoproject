import { DatePipe } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { TaskDto } from '../../models/task.model';
import { PriorityBadgeComponent } from '../priority-badge/priority-badge.component';

@Component({
  selector: 'app-task-card',
  standalone: true,
  imports: [PriorityBadgeComponent, DatePipe],
  templateUrl: './task-card.component.html'
})
export class TaskCardComponent {
  @Input({ required: true }) task!: TaskDto;
  @Output() edit = new EventEmitter<TaskDto>();
  @Output() delete = new EventEmitter<TaskDto>();

  get isOverdue(): boolean {
    if (!this.task.dueDate || this.task.status === 'Done') {
      return false;
    }
    return new Date(this.task.dueDate) < new Date(new Date().toDateString());
  }
}

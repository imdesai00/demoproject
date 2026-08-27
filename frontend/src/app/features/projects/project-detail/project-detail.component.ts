import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { finalize, forkJoin } from 'rxjs';
import { ProjectsService } from '../../../core/services/projects.service';
import { TasksService } from '../../../core/services/tasks.service';
import { AlertComponent } from '../../../shared/components/alert/alert.component';
import { SpinnerComponent } from '../../../shared/components/spinner/spinner.component';
import { TaskCardComponent } from '../../../shared/components/task-card/task-card.component';
import { ProjectDto } from '../../../shared/models/project.model';
import { ProjectTaskStatus, TaskDto, TaskPriority } from '../../../shared/models/task.model';
import { extractErrorMessage } from '../../../shared/utils/error-message';

interface Column {
  status: ProjectTaskStatus;
  title: string;
  tasks: TaskDto[];
}

@Component({
  selector: 'app-project-detail',
  standalone: true,
  imports: [DragDropModule, ReactiveFormsModule, RouterLink, AlertComponent, SpinnerComponent, TaskCardComponent],
  templateUrl: './project-detail.component.html'
})
export class ProjectDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly projectsService = inject(ProjectsService);
  private readonly tasksService = inject(TasksService);
  private readonly fb = inject(FormBuilder);

  readonly project = signal<ProjectDto | null>(null);
  readonly isLoading = signal(true);
  readonly loadError = signal('');
  readonly actionError = signal('');

  readonly columns = signal<Column[]>([
    { status: 'Todo', title: 'To Do', tasks: [] },
    { status: 'InProgress', title: 'In Progress', tasks: [] },
    { status: 'Done', title: 'Done', tasks: [] }
  ]);

  readonly isFormOpen = signal(false);
  readonly isSavingTask = signal(false);
  readonly editingTaskId = signal<string | null>(null);
  readonly formError = signal('');

  readonly priorities: TaskPriority[] = ['Low', 'Medium', 'High'];

  readonly taskForm = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: [''],
    priority: ['Medium' as TaskPriority, [Validators.required]],
    dueDate: ['']
  });

  private projectId!: string;

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('id')!;
    this.loadData();
  }

  get connectedDropListIds(): string[] {
    return this.columns().map((c) => `list-${c.status}`);
  }

  loadData(): void {
    this.isLoading.set(true);
    this.loadError.set('');

    forkJoin({
      project: this.projectsService.getById(this.projectId),
      tasks: this.tasksService.getAll(this.projectId)
    })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ project, tasks }) => {
          this.project.set(project);
          this.columns.set([
            { status: 'Todo', title: 'To Do', tasks: tasks.filter((t) => t.status === 'Todo') },
            { status: 'InProgress', title: 'In Progress', tasks: tasks.filter((t) => t.status === 'InProgress') },
            { status: 'Done', title: 'Done', tasks: tasks.filter((t) => t.status === 'Done') }
          ]);
        },
        error: (error) => {
          if (error?.status === 404 || error?.status === 403) {
            this.router.navigate(['/dashboard']);
            return;
          }
          this.loadError.set(extractErrorMessage(error, 'Could not load this project.'));
        }
      });
  }

  drop(event: CdkDragDrop<TaskDto[]>, targetStatus: ProjectTaskStatus): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      return;
    }

    const task = event.previousContainer.data[event.previousIndex];
    transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);

    this.tasksService.updateStatus(this.projectId, task.id, { status: targetStatus }).subscribe({
      next: (updated) => {
        task.status = updated.status;
        task.updatedAt = updated.updatedAt;
      },
      error: (error) => {
        transferArrayItem(event.container.data, event.previousContainer.data, event.currentIndex, event.previousIndex);
        this.actionError.set(extractErrorMessage(error, 'Could not update the task status.'));
      }
    });
  }

  openCreateForm(): void {
    this.editingTaskId.set(null);
    this.formError.set('');
    this.taskForm.reset({ title: '', description: '', priority: 'Medium', dueDate: '' });
    this.isFormOpen.set(true);
  }

  openEditForm(task: TaskDto): void {
    this.editingTaskId.set(task.id);
    this.formError.set('');
    this.taskForm.reset({
      title: task.title,
      description: task.description ?? '',
      priority: task.priority,
      dueDate: task.dueDate ?? ''
    });
    this.isFormOpen.set(true);
  }

  closeForm(): void {
    this.isFormOpen.set(false);
  }

  submitForm(): void {
    if (this.taskForm.invalid) {
      this.taskForm.markAllAsTouched();
      return;
    }

    const { title, description, priority, dueDate } = this.taskForm.getRawValue();
    const payload = { title, description: description || null, priority, dueDate: dueDate || null };

    this.isSavingTask.set(true);
    this.formError.set('');

    const editingId = this.editingTaskId();
    const request = editingId
      ? this.tasksService.update(this.projectId, editingId, payload)
      : this.tasksService.create(this.projectId, payload);

    request.pipe(finalize(() => this.isSavingTask.set(false))).subscribe({
      next: (task) => {
        this.applyTaskUpsert(task, editingId);
        this.isFormOpen.set(false);
      },
      error: (error) => this.formError.set(extractErrorMessage(error, 'Could not save the task.'))
    });
  }

  deleteTask(task: TaskDto): void {
    if (!confirm(`Delete "${task.title}"?`)) {
      return;
    }

    this.tasksService.delete(this.projectId, task.id).subscribe({
      next: () => {
        this.columns.update((columns) =>
          columns.map((c) => ({ ...c, tasks: c.tasks.filter((t) => t.id !== task.id) }))
        );
      },
      error: (error) => this.actionError.set(extractErrorMessage(error, 'Could not delete the task.'))
    });
  }

  private applyTaskUpsert(task: TaskDto, editingId: string | null): void {
    this.columns.update((columns) =>
      columns.map((column) => {
        if (editingId) {
          return {
            ...column,
            tasks: column.status === task.status
              ? column.tasks.map((t) => (t.id === task.id ? task : t))
              : column.tasks.filter((t) => t.id !== task.id)
          };
        }
        return column.status === task.status ? { ...column, tasks: [...column.tasks, task] } : column;
      })
    );
  }
}

import { Component, OnInit, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { DashboardService } from '../../core/services/dashboard.service';
import { ProjectsService } from '../../core/services/projects.service';
import { AlertComponent } from '../../shared/components/alert/alert.component';
import { EmptyStateComponent } from '../../shared/components/empty-state/empty-state.component';
import { SpinnerComponent } from '../../shared/components/spinner/spinner.component';
import { DashboardSummaryDto } from '../../shared/models/dashboard.model';
import { extractErrorMessage } from '../../shared/utils/error-message';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink, AlertComponent, EmptyStateComponent, SpinnerComponent],
  templateUrl: './dashboard.component.html'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly projectsService = inject(ProjectsService);
  private readonly fb = inject(FormBuilder);

  readonly summary = signal<DashboardSummaryDto | null>(null);
  readonly isLoading = signal(true);
  readonly loadError = signal('');

  readonly isCreating = signal(false);
  readonly isSavingProject = signal(false);
  readonly createError = signal('');

  readonly createForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['']
  });

  ngOnInit(): void {
    this.loadSummary();
  }

  loadSummary(): void {
    this.isLoading.set(true);
    this.loadError.set('');

    this.dashboardService
      .getSummary()
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: (summary) => this.summary.set(summary),
        error: (error) => this.loadError.set(extractErrorMessage(error, 'Could not load your dashboard.'))
      });
  }

  startCreating(): void {
    this.isCreating.set(true);
    this.createError.set('');
    this.createForm.reset({ name: '', description: '' });
  }

  cancelCreating(): void {
    this.isCreating.set(false);
  }

  submitCreate(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isSavingProject.set(true);
    this.createError.set('');

    const { name, description } = this.createForm.getRawValue();

    this.projectsService
      .create({ name, description: description || null })
      .pipe(finalize(() => this.isSavingProject.set(false)))
      .subscribe({
        next: () => {
          this.isCreating.set(false);
          this.loadSummary();
        },
        error: (error) => this.createError.set(extractErrorMessage(error, 'Could not create the project.'))
      });
  }

  deleteProject(projectId: string, event: Event): void {
    event.preventDefault();
    event.stopPropagation();

    if (!confirm('Delete this project and all of its tasks? This cannot be undone.')) {
      return;
    }

    this.projectsService.delete(projectId).subscribe({
      next: () => this.loadSummary(),
      error: (error) => this.loadError.set(extractErrorMessage(error, 'Could not delete the project.'))
    });
  }
}

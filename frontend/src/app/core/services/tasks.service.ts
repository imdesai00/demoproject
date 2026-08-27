import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CreateTaskRequest,
  TaskDto,
  UpdateTaskRequest,
  UpdateTaskStatusRequest,
} from '../../shared/models/task.model';

@Injectable({ providedIn: 'root' })
export class TasksService {
  private readonly http = inject(HttpClient);

  private baseUrl(projectId: string): string {
    return `${environment.apiUrl}/projects/${projectId}/tasks`;
  }

  getAll(projectId: string): Observable<TaskDto[]> {
    return this.http.get<TaskDto[]>(this.baseUrl(projectId));
  }

  create(projectId: string, request: CreateTaskRequest): Observable<TaskDto> {
    return this.http.post<TaskDto>(this.baseUrl(projectId), request);
  }

  update(projectId: string, taskId: string, request: UpdateTaskRequest): Observable<TaskDto> {
    return this.http.put<TaskDto>(`${this.baseUrl(projectId)}/${taskId}`, request);
  }

  updateStatus(
    projectId: string,
    taskId: string,
    request: UpdateTaskStatusRequest,
  ): Observable<TaskDto> {
    return this.http.patch<TaskDto>(`${this.baseUrl(projectId)}/${taskId}/status`, request);
  }

  delete(projectId: string, taskId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl(projectId)}/${taskId}`);
  }
}

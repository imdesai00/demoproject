export interface TaskStatusCounts {
  todo: number;
  inProgress: number;
  done: number;
  total: number;
}

export interface ProjectDto {
  id: string;
  name: string;
  description: string | null;
  createdAt: string;
  updatedAt: string;
  taskCounts: TaskStatusCounts;
}

export interface CreateProjectRequest {
  name: string;
  description: string | null;
}

export interface UpdateProjectRequest {
  name: string;
  description: string | null;
}

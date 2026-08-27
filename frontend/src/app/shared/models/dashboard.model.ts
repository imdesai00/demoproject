import { ProjectDto, TaskStatusCounts } from './project.model';

export interface DashboardSummaryDto {
  totalProjects: number;
  totalTasks: number;
  overallTaskCounts: TaskStatusCounts;
  projects: ProjectDto[];
}

import { HttpErrorResponse } from '@angular/common/http';
import { ApiErrorResponse } from '../models/api-error.model';

export function extractErrorMessage(error: unknown, fallback = 'Something went wrong. Please try again.'): string {
  if (error instanceof HttpErrorResponse) {
    const body = error.error as ApiErrorResponse | undefined;
    if (body?.errors) {
      return Object.values(body.errors).flat().join(' ');
    }
    if (body?.message) {
      return body.message;
    }
  }
  return fallback;
}

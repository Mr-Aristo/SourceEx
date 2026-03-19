import { HttpErrorResponse } from '@angular/common/http';

export function getHttpErrorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (typeof error.error === 'string' && error.error.trim().length > 0) {
      return error.error;
    }

    if (typeof error.error?.title === 'string' && error.error.title.trim().length > 0) {
      return error.error.title;
    }

    if (error.status === 0) {
      return 'The API could not be reached. Verify that SourceEx.API is running.';
    }

    return `Request failed with status ${error.status}.`;
  }

  return 'An unexpected client error occurred.';
}


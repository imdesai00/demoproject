import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { authGuard, guestGuard } from './auth.guard';

describe('authGuard', () => {
  let authServiceMock: { isAuthenticated: () => boolean };

  beforeEach(() => {
    authServiceMock = { isAuthenticated: () => false };

    TestBed.configureTestingModule({
      providers: [provideRouter([]), { provide: AuthService, useValue: authServiceMock }]
    });
  });

  function runAuthGuard() {
    return TestBed.runInInjectionContext(() => authGuard({} as never, {} as never));
  }

  function runGuestGuard() {
    return TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));
  }

  it('allows access when authenticated', () => {
    authServiceMock.isAuthenticated = () => true;
    expect(runAuthGuard()).toBe(true);
  });

  it('redirects to /login when not authenticated', () => {
    authServiceMock.isAuthenticated = () => false;
    const result = runAuthGuard();
    expect(result).not.toBe(true);
    expect(result?.toString()).toContain('/login');
  });

  it('guestGuard redirects authenticated users to /dashboard', () => {
    authServiceMock.isAuthenticated = () => true;
    const result = runGuestGuard();
    expect(result).not.toBe(true);
    expect(result?.toString()).toContain('/dashboard');
  });

  it('guestGuard allows anonymous users through', () => {
    authServiceMock.isAuthenticated = () => false;
    expect(runGuestGuard()).toBe(true);
  });
});

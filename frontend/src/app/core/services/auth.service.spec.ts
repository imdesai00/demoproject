import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { AuthResponse } from '../../shared/models/auth.model';
import { AuthService } from './auth.service';
import { TokenStorageService } from './token-storage.service';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let tokenStorage: TokenStorageService;

  const authResponse: AuthResponse = {
    accessToken: 'access-token',
    accessTokenExpiresAt: new Date().toISOString(),
    refreshToken: 'refresh-token',
    refreshTokenExpiresAt: new Date().toISOString(),
    user: { id: '1', email: 'user@example.com', displayName: 'User' }
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    tokenStorage = TestBed.inject(TokenStorageService);
    tokenStorage.clear();
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('starts unauthenticated when no session is stored', () => {
    expect(service.isAuthenticated()).toBe(false);
  });

  it('stores the session and becomes authenticated after login', () => {
    service.login({ email: 'user@example.com', password: 'Password123!' }).subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/auth/login`);
    expect(req.request.method).toBe('POST');
    req.flush(authResponse);

    expect(service.isAuthenticated()).toBe(true);
    expect(service.currentUser()?.email).toBe('user@example.com');
    expect(service.getAccessToken()).toBe('access-token');
  });

  it('clears the session on logout', () => {
    service.login({ email: 'user@example.com', password: 'Password123!' }).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/login`).flush(authResponse);

    service.logout().subscribe();
    httpMock.expectOne(`${environment.apiUrl}/auth/logout`).flush(null);

    expect(service.isAuthenticated()).toBe(false);
    expect(service.getAccessToken()).toBeNull();
  });
});

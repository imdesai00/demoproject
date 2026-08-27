import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { LoginComponent } from './login.component';

describe('LoginComponent', () => {
  let fixture: ComponentFixture<LoginComponent>;
  let component: LoginComponent;
  let authServiceMock: { login: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authServiceMock = { login: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideRouter([{ path: 'dashboard', children: [] }]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: authServiceMock }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
  });

  it('does not call the API when the form is invalid', () => {
    component.submit();
    expect(authServiceMock.login).not.toHaveBeenCalled();
  });

  it('shows an error message when login fails', () => {
    authServiceMock.login.mockReturnValue(throwError(() => new Error('bad credentials')));

    component.form.setValue({ email: 'user@example.com', password: 'wrong-password' });
    component.submit();

    expect(authServiceMock.login).toHaveBeenCalledWith({ email: 'user@example.com', password: 'wrong-password' });
    expect(component.errorMessage()).not.toBe('');
    expect(component.isSubmitting()).toBe(false);
  });

  it('calls the API when the form is valid', () => {
    authServiceMock.login.mockReturnValue(of({} as never));

    component.form.setValue({ email: 'user@example.com', password: 'Password123!' });
    component.submit();

    expect(authServiceMock.login).toHaveBeenCalledTimes(1);
  });
});

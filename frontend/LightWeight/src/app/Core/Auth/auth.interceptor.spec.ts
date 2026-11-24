import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { authInterceptor } from './auth.interceptor';
import { TokenStorageService } from './token-storage.service';
import { environment } from '../../../Environments/Environment';

const refreshUrl = `${environment.apiUrl}/auth/refresh`;
/** Lets the promise of the refresh and the retry run */
const tick = () => new Promise(resolve => setTimeout(resolve));

describe('authInterceptor', () => {
  let http: HttpClient;
  let backend: HttpTestingController;
  let tokens: TokenStorageService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(withInterceptors([authInterceptor])), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpClient);
    backend = TestBed.inject(HttpTestingController);
    tokens = TestBed.inject(TokenStorageService);
    tokens.setAccessToken('old');
  });

  afterEach(() => backend.verify());

  /** The 401 arrives after the interceptor returned, outside the injection context */
  it('refreshes the token on a 401 and retries the request with it', async () => {
    const result = firstValueFrom(http.get<string>('/api/sets'));

    const first = backend.expectOne('/api/sets');
    expect(first.request.headers.get('Authorization')).toBe('Bearer old');
    first.flush(null, { status: 401, statusText: 'Unauthorized' });

    await tick();
    backend.expectOne(refreshUrl).flush('new');
    await tick();

    const retry = backend.expectOne('/api/sets');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new');
    retry.flush('ok');

    expect(await result).toBe('ok');
    expect(tokens.getAccessToken()).toBe('new');
  });

  it('keeps refreshing after the first expiry: a second 401 later also gets a new token', async () => {
    for (const [previous, next] of [['old', 'second'], ['second', 'third']]) {
      const result = firstValueFrom(http.get<string>('/api/sets'));
      const first = backend.expectOne('/api/sets');
      expect(first.request.headers.get('Authorization')).toBe(`Bearer ${previous}`);
      first.flush(null, { status: 401, statusText: 'Unauthorized' });

      await tick();
      backend.expectOne(refreshUrl).flush(next);
      await tick();

      backend.expectOne('/api/sets').flush('ok');
      expect(await result).toBe('ok');
    }
  });

  it('fails the request and clears the token when the refresh cookie is no longer valid', async () => {
    const result = firstValueFrom(http.get<string>('/api/sets'));
    backend.expectOne('/api/sets').flush(null, { status: 401, statusText: 'Unauthorized' });

    await tick();
    backend.expectOne(refreshUrl).flush(null, { status: 401, statusText: 'Unauthorized' });

    await expect(result).rejects.toMatchObject({ status: 401 });
    expect(tokens.getAccessToken()).toBeNull();
  });
});

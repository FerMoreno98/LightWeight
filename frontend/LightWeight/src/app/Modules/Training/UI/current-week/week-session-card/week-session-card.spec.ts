import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Session } from '../../../data/training-api.service';
import { WeekSessionCard, WeekSessionStatus } from './week-session-card';

const session: Session = { id: 's1', name: 'Pull', setsCount: 22, exercisesCount: 6 };

async function render(status: WeekSessionStatus, extra: Record<string, unknown> = {}) {
  await TestBed.configureTestingModule({ imports: [WeekSessionCard], providers: [provideRouter([])] }).compileComponents();
  const fixture = TestBed.createComponent(WeekSessionCard);
  fixture.componentRef.setInput('session', session);
  fixture.componentRef.setInput('trainingTemplateId', 't1');
  fixture.componentRef.setInput('status', status);
  fixture.componentRef.setInput('position', 2);
  for (const [key, value] of Object.entries(extra)) fixture.componentRef.setInput(key, value);
  await fixture.whenStable();
  return { card: fixture.componentInstance, el: fixture.nativeElement as HTMLElement };
}

describe('WeekSessionCard', () => {
  it('says the day a done session was finished', async () => {
    const { card } = await render('done', { finishedAt: '2026-11-16T18:30:00Z' });

    expect(card.stateLabel()).toBe('hecha el lun 16');
  });

  it('shows the sets done over the planned ones while in progress', async () => {
    const { el } = await render('inProgress', {
      volume: { Back: 10, Biceps: 6 },
      progress: { sets: 10, byGroup: { Back: 6, Biceps: 4 } },
    });

    expect(el.textContent).toContain('series hechas');
    expect(el.textContent?.replace(/\s+/g, '')).toContain('10/22');
    expect(el.textContent?.replace(/\s+/g, '')).toContain('6/10');
  });

  it('shows its position while pending and opens the session', async () => {
    const { el } = await render('pending', { next: true });

    expect(el.textContent).toContain('Siguiente');
    expect(el.querySelector('[aria-hidden="true"]')?.textContent?.trim()).toBe('2');
    expect(el.querySelector('a')?.getAttribute('href')).toBe('/training/session/t1/s1');
  });
});

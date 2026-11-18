import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DashboardApiService, SessionPerformance, WeeklyVolume } from '../../data/dashboard-api.service';
import { TrainingApiService } from '../../../Training/data/training-api.service';
import { TrainingOverview } from './training-overview';

const session: SessionPerformance = {
  sessionId: 's1', name: 'Torso A', startedAt: '2026-10-05T17:30:00Z', performance: 2.5, comparedExercises: 3,
  motivation: 8, sleep: 7, doms: 3, excluded: [],
};
const week: WeeklyVolume = { microcycleId: 'w1', weekNumber: 1, startedAt: '2026-10-05T17:30:00Z', setsByMuscle: { Chest: 10 } };

async function render(volume: WeeklyVolume[], history: SessionPerformance[]) {
  await TestBed.configureTestingModule({
    imports: [TrainingOverview],
    providers: [
      { provide: DashboardApiService, useValue: {
        GetTrainingVolume: () => of(volume),
        // The new macrocycle has no session yet; the history has the ones of the previous one
        GetSessionsPerformance: (macrocycleId: string | null = null) => of(macrocycleId ? [] : history),
      } },
      { provide: TrainingApiService, useValue: { GetCurrentMicrocycle: () => of({ macrocycleId: 'new' }) } },
    ],
  }).compileComponents();
  const fixture = TestBed.createComponent(TrainingOverview);
  await fixture.whenStable();
  await new Promise(resolve => setTimeout(resolve));
  await fixture.whenStable();
  return fixture.nativeElement as HTMLElement;
}

describe('TrainingOverview', () => {
  it('in a macrocycle just started it keeps the period filter and offers the history', async () => {
    const el = await render([week], [session]);

    expect(el.querySelector('app-scope-toggle')).not.toBeNull();
    expect(el.textContent).toContain('Todavía no has terminado ninguna sesión en este macrociclo');
    expect(el.textContent).toContain('Volumen semanal');
    expect(el.textContent).not.toContain('Aún no hay sesiones terminadas');
  });

  it('says there is nothing yet only when no session was ever finished', async () => {
    const el = await render([], []);

    expect(el.textContent).toContain('Aún no hay sesiones terminadas');
    expect(el.querySelector('app-scope-toggle')).toBeNull();
  });
});

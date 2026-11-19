import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DashboardApiService, SessionPerformance, WeeklyVolume } from '../../data/dashboard-api.service';
import { TrainingApiService } from '../../../Training/data/training-api.service';
import { TrainingOverview } from './training-overview';

const session: SessionPerformance = {
  sessionId: 's1', name: 'Torso A', startedAt: '2026-10-05T17:30:00Z', performance: 2.5, comparedExercises: 3,
  motivation: 8, sleep: 7, doms: 3, excluded: [], nutrition: null,
};
const week: WeeklyVolume = { microcycleId: 'w1', weekNumber: 1, startedAt: '2026-10-05T17:30:00Z', setsByMuscle: { Chest: 10 } };

async function render(volume: WeeklyVolume[], history: SessionPerformance[], macrocycleId: string | null = 'new') {
  await TestBed.configureTestingModule({
    imports: [TrainingOverview],
    providers: [
      { provide: DashboardApiService, useValue: {
        GetTrainingVolume: () => of(volume),
        // The new macrocycle has no session yet; the history has the ones of the previous one
        GetSessionsPerformance: (macrocycleId: string | null = null) => of(macrocycleId ? [] : history),
      } },
      { provide: TrainingApiService, useValue: { GetCurrentMicrocycle: () => of({ macrocycleId }) } },
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

  const withDiary = (id: string, startedAt: string, calories: number): SessionPerformance => ({
    ...session, sessionId: id, startedAt,
    nutrition: { previousDayCalories: calories, previousDayCarbohydratePerKg: 3.2, sameDayCalories: 2500, proteinPerKgLast7Days: 1.9 },
  });

  it('offers the Nutrición tab only when some session has diary around it', async () => {

    const without = await render([week], [session], null);
    expect(without.querySelector('[role=tablist]')).toBeNull();
    TestBed.resetTestingModule();

    const el = await render([week], [session, withDiary('s2', '2026-10-07T17:30:00Z', 2000), withDiary('s3', '2026-10-09T17:30:00Z', 2200)], null);
    const tabs = [...el.querySelectorAll<HTMLButtonElement>('[role=tab]')];
    expect(tabs.map(t => t.textContent?.trim())).toEqual(['Bienestar', 'Nutrición']);
    expect(el.textContent).toContain('Sueño');
    tabs[1].click();
    await new Promise(resolve => setTimeout(resolve));
    expect(el.textContent).toContain('Kcal · día anterior');
    expect(el.textContent).toContain('tu media 2100');
  });

  it('needs two sessions with diary on two different days before drawing the nutrition rows', async () => {
    // Two sessions of the same day share the same day of diary: still one record
    const el = await render([week], [withDiary('s2', '2026-10-07T07:30:00Z', 2000), withDiary('s3', '2026-10-07T18:30:00Z', 2000)], null);
    el.querySelectorAll<HTMLButtonElement>('[role=tab]')[1].click();
    await new Promise(resolve => setTimeout(resolve));

    expect(el.textContent).toContain('Aún hay poco para comparar');
    expect(el.textContent).not.toContain('Kcal · día anterior');
  });

  it('says there is nothing yet only when no session was ever finished', async () => {
    const el = await render([], []);

    expect(el.textContent).toContain('Aún no hay sesiones terminadas');
    expect(el.querySelector('app-scope-toggle')).toBeNull();
  });
});

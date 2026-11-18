import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { DashboardApiService } from '../data/dashboard-api.service';
import { TrainingApiService } from '../../Training/data/training-api.service';
import { TrainingOverviewStore } from './training-overview.store';

function setup(macrocycleId: string | null) {
  const requested: (string | null)[] = [];
  TestBed.configureTestingModule({
    providers: [
      { provide: DashboardApiService, useValue: {
        GetTrainingVolume: () => of([]),
        GetSessionsPerformance: (id: string | null = null) => { requested.push(id); return of([]); },
      } },
      { provide: TrainingApiService, useValue: { GetCurrentMicrocycle: () => of({ macrocycleId }) } },
    ],
  });
  return { store: TestBed.inject(TrainingOverviewStore), requested };
}

describe('TrainingOverviewStore', () => {
  it('shows the performance of the current macrocycle by default and the history on request', async () => {
    const { store, requested } = setup('m1');

    await store.Load();
    await store.SetPerformanceScope('history');

    expect(requested).toEqual(['m1', null]);
    expect(store.performanceScope()).toBe('history');
  });

  it('without an active macrocycle it can only show the history', async () => {
    const { store, requested } = setup(null);

    await store.Load();

    expect(store.hasMacrocycle()).toBe(false);
    expect(store.performanceScope()).toBe('history');
    expect(requested).toEqual([null]);
  });
});

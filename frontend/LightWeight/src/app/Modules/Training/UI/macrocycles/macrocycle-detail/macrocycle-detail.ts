import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';
import { MacrocycleMesocycle } from '../../../data/training-api.service';
import { TRAINING_STAGE_LABELS, formatCyclePeriod } from '../../shared/cycle-format';
import { ConfirmDialog } from '../../../../../Shared/confirm-dialog/confirm-dialog';

@Component({
  selector: 'app-macrocycle-detail',
  standalone: true,
  imports: [FormsModule, RouterLink, ConfirmDialog],
  templateUrl: './macrocycle-detail.html',
  styleUrl: './macrocycle-detail.css',
})
export class MacrocycleDetail {
  private store = inject(TrainingStore);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  macrocycleId = '';
  macrocycle = this.store.macrocycleDetail;
  programs = this.store.programs;
  isLoading = this.store.isLoading;
  error = this.store.error;

  isFinished = computed(() => !!this.macrocycle()?.finishedAt);
  activeMesocycle = computed(() => this.macrocycle()?.mesocycles.find(m => !m.finishedAt) ?? null);
  /** A new mesocycle can only start in an active macrocycle once the current mesocycle is finished */
  canAddMesocycle = computed(() => !!this.macrocycle() && !this.isFinished() && !this.activeMesocycle());

  isAdding = false;
  isFinishing = signal(false);
  isConfirmingFinish = signal(false);

  finishMessage = computed(() => this.activeMesocycle()
    ? 'También se finalizará el mesociclo en curso. Después podrás empezar un macrociclo nuevo. Esta acción no se puede deshacer.'
    : 'Después podrás empezar un macrociclo nuevo. Esta acción no se puede deshacer.');

  // Mesocycle form
  programId = '';
  motivationLevel = 7;
  injuries = '';
  comments = '';

  async ngOnInit() {
    this.macrocycleId = this.route.snapshot.paramMap.get('id')!;
    await Promise.all([
      this.store.GetMacrocycleDetail(this.macrocycleId),
      this.store.GetUserPrograms(),
    ]);
  }

  stageLabel(): string {
    const stage = this.macrocycle()?.stage;
    return stage ? TRAINING_STAGE_LABELS[stage] ?? stage : '';
  }

  macrocyclePeriod(): string {
    const macrocycle = this.macrocycle();
    return macrocycle ? formatCyclePeriod(macrocycle.startedAt, macrocycle.finishedAt) : '';
  }

  mesocyclePeriod(mesocycle: MacrocycleMesocycle): string {
    return formatCyclePeriod(mesocycle.startedAt, mesocycle.finishedAt);
  }

  openMesocycle(mesocycle: MacrocycleMesocycle) {
    this.router.navigate(['/training/mesocycle', mesocycle.id]);
  }

  showAddForm() {
    this.programId = this.programs().length === 1 ? this.programs()[0].id : '';
    this.motivationLevel = 7;
    this.injuries = '';
    this.comments = '';
    this.isAdding = true;
  }

  backToMacrocycle() {
    this.isAdding = false;
  }

  async onSubmitMesocycle() {
    const id = await this.store.CreateMesocycle(
      this.macrocycleId,
      this.programId,
      this.motivationLevel,
      this.injuries.trim() || null,
      this.comments.trim() || null,
    );
    if (id) {
      this.router.navigate(['/training/mesocycle', id]);
    }
  }

  askFinishMacrocycle() {
    this.isConfirmingFinish.set(true);
  }

  cancelFinishMacrocycle() {
    this.isConfirmingFinish.set(false);
  }

  async finishMacrocycle() {
    this.isFinishing.set(true);
    try {
      await this.store.FinishMacrocycle(this.macrocycleId);
    } finally {
      this.isFinishing.set(false);
      this.isConfirmingFinish.set(false);
    }
  }
}

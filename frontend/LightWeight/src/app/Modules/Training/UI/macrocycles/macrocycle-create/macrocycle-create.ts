import { Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { TrainingStore } from '../../../state/training.store';

@Component({
  selector: 'app-macrocycle-create',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './macrocycle-create.html',
  styleUrl: './macrocycle-create.css',
})
export class MacrocicloCreatePage {
  private store = inject(TrainingStore);
  private router = inject(Router);

  isLoading = this.store.isLoading;
  error = this.store.error;

  private _loaded = signal(false);
  loaded = this._loaded.asReadonly();
  /** Only one macrocycle can be active: if there is one, the form is not shown */
  activeMacrocycle = computed(() => this.store.macrocycles().find(m => !m.finishedAt) ?? null);

  stage = '';
  comments = '';

  async ngOnInit() {
    await this.store.GetUserMacrocycles();
    this._loaded.set(true);
  }

  async onSubmit() {
    const id = await this.store.CreateMacrocycle(this.stage, this.comments.trim() || null);
    if (id) {
      this.router.navigate(['/training/macrocycle', id]);
    }
  }

  cancel() {
    this.router.navigate(['/training/macrocycles']);
  }
}

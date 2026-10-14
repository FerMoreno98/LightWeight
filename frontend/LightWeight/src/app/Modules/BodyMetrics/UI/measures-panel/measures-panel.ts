import { Component, computed, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { BodyMetricsStore } from '../../state/body-metrics.store';
import { MeasurementEntry, MeasureValues } from '../../data/body-metrics-api.service';
import { MeasureField, MeasurementConfig } from '../measurement-config';
import { ConfirmDialog } from '../../../../Shared/confirm-dialog/confirm-dialog';

/** Fields of the form grouped by section, in order */
interface FieldGroup {
  name: string | null;
  fields: MeasureField[];
}

const VALUE_FORMAT = new Intl.NumberFormat('es-ES', { maximumFractionDigits: 2 });
const DATE_FORMAT = new Intl.DateTimeFormat('es-ES', { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' });

/** Margin for the difference between this device's clock and the server's (the backend allows 5 minutes) */
const CLOCK_TOLERANCE_MS = 5 * 60 * 1000;

/**
 * Registers, lists, corrects and deletes one kind of measurement (weight, perimeters or skinfolds).
 * What changes between kinds (fields, unit, limits) comes from the config
 */
@Component({
  selector: 'app-measures-panel',
  standalone: true,
  imports: [FormsModule, ConfirmDialog],
  templateUrl: './measures-panel.html',
  styleUrl: './measures-panel.css',
})
export class MeasuresPanel {
  private store = inject(BodyMetricsStore);

  config = input.required<MeasurementConfig>();

  entries = computed(() => this.store.history()[this.config().kind]);
  loaded = computed(() => this.store.loaded()[this.config().kind]);
  error = this.store.error;

  groups = computed<FieldGroup[]>(() => {
    const groups: FieldGroup[] = [];
    for (const field of this.config().fields) {
      const name = field.group ?? null;
      const group = groups.find(g => g.name === name);
      if (group) group.fields.push(field);
      else groups.push({ name, fields: [field] });
    }
    return groups;
  });

  // ---------------------------------------------------------------- Form

  values = signal<MeasureValues>({});
  measuredAt = signal(toLocalInput(new Date()));
  /** Measurement being corrected, null when registering a new one */
  editing = signal<MeasurementEntry | null>(null);
  isSaving = signal(false);

  /** Fields whose value is outside the limits of the config */
  invalidFields = computed(() => {
    const { min, max, minInclusive } = this.config();
    return this.config().fields
      .filter(f => {
        const value = this.values()[f.key];
        if (value === null || value === undefined) return false;
        return (minInclusive ? value < min : value <= min) || value > max;
      })
      .map(f => f.key);
  });
  hasAnyValue = computed(() => this.config().fields.some(f => this.values()[f.key] != null));
  isInTheFuture = computed(() => {
    const date = new Date(this.measuredAt());
    return !isNaN(date.getTime()) && date.getTime() > Date.now() + CLOCK_TOLERANCE_MS;
  });
  canSave = computed(() =>
    this.hasAnyValue() && this.invalidFields().length === 0 && !!this.measuredAt() && !this.isInTheFuture() && !this.isSaving());
  /** "entre 0 y 300 cm" / "entre 20 y 400 kg" */
  rangeLabel = computed(() => {
    const { min, max, unit, minInclusive } = this.config();
    return minInclusive ? `entre ${min} y ${max} ${unit}` : `mayor que ${min} y hasta ${max} ${unit}`;
  });
  maxDate = toLocalInput(new Date());

  // ---------------------------------------------------------------- Delete

  deleting = signal<MeasurementEntry | null>(null);
  isDeleting = signal(false);

  async ngOnInit() {
    if (!this.loaded()) await this.store.GetHistory(this.config().kind);
  }

  setValue(key: string, value: number | string | null) {
    const parsed = value === '' || value === null ? null : Number(value);
    this.values.update(v => ({ ...v, [key]: parsed !== null && isNaN(parsed) ? null : parsed }));
  }

  isInvalid(key: string): boolean {
    return this.invalidFields().includes(key);
  }

  async save() {
    if (!this.canSave()) return;
    const kind = this.config().kind;
    // Every field is sent: the ones left empty go as null (a correction replaces the whole measurement)
    const values: MeasureValues = {};
    for (const field of this.config().fields) values[field.key] = this.values()[field.key] ?? null;
    const measuredAt = new Date(this.measuredAt()).toISOString();

    this.isSaving.set(true);
    try {
      const editing = this.editing();
      const saved = editing
        ? await this.store.Correct(kind, editing.id, values, measuredAt)
        : await this.store.Register(kind, values, measuredAt);
      if (saved) this.resetForm();
    } finally {
      this.isSaving.set(false);
    }
  }

  /** Loads a measurement in the form to correct it */
  edit(entry: MeasurementEntry) {
    const values: MeasureValues = {};
    for (const field of this.config().fields) values[field.key] = (entry[field.key] as number | null | undefined) ?? null;
    this.values.set(values);
    this.measuredAt.set(toLocalInput(new Date(entry.measuredAt)));
    this.editing.set(entry);
    this.store.clearError();
  }

  resetForm() {
    this.values.set({});
    this.measuredAt.set(toLocalInput(new Date()));
    this.maxDate = toLocalInput(new Date());
    this.editing.set(null);
  }

  async confirmDelete() {
    const entry = this.deleting();
    if (!entry) return;
    this.isDeleting.set(true);
    try {
      if (await this.store.Delete(this.config().kind, entry.id) && this.editing()?.id === entry.id) this.resetForm();
    } finally {
      this.isDeleting.set(false);
      this.deleting.set(null);
    }
  }

  // ---------------------------------------------------------------- History

  /** Fields measured in an entry, with their label */
  measured(entry: MeasurementEntry): { label: string; value: string }[] {
    return this.config().fields
      .filter(f => entry[f.key] != null)
      .map(f => ({ label: f.label, value: this.format(entry[f.key] as number) }));
  }

  summary(entry: MeasurementEntry): string | null {
    const summary = this.config().summary;
    const value = summary ? entry[summary.key] : null;
    return value == null ? null : this.format(value as number);
  }

  format(value: number): string {
    return `${VALUE_FORMAT.format(value)} ${this.config().unit}`;
  }

  formatDate(isoDate: string): string {
    return DATE_FORMAT.format(new Date(isoDate));
  }
}

/** "2026-10-14T08:30" in the local time of the device, as datetime-local inputs expect */
function toLocalInput(date: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

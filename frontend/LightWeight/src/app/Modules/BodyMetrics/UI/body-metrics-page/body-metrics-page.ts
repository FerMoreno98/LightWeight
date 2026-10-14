import { Component, signal } from '@angular/core';
import { MeasuresPanel } from '../measures-panel/measures-panel';
import { MEASUREMENT_CONFIGS, MeasurementConfig } from '../measurement-config';
import { MeasurementKind } from '../../data/body-metrics-api.service';

/** Body measurements of the user: weight, perimeters and skinfolds, one tab each */
@Component({
  selector: 'app-body-metrics-page',
  standalone: true,
  imports: [MeasuresPanel],
  templateUrl: './body-metrics-page.html',
  styleUrl: './body-metrics-page.css',
})
export class BodyMetricsPage {
  readonly configs = MEASUREMENT_CONFIGS;
  selected = signal<MeasurementKind>('weights');

  select(config: MeasurementConfig) {
    this.selected.set(config.kind);
  }
}

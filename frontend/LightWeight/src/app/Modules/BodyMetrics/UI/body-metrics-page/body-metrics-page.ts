import { Component, signal } from '@angular/core';
import { MeasuresPanel } from '../measures-panel/measures-panel';
import { MEASUREMENT_CONFIGS } from '../measurement-config';
import { MeasurementKind } from '../../data/body-metrics-api.service';
import { BodyMetricsCharts } from '../../../Dashboard/UI/body-metrics-charts/body-metrics-charts';

/** A kind of measurement, or the charts with the evolution of all of them */
export type BodyMetricsTab = MeasurementKind | 'evolution';

/**
 * Body measurements of the user: weight, perimeters and skinfolds, one tab each, plus their evolution.
 * The charts belong to the Dashboard module (they read its projections) and are embedded here
 */
@Component({
  selector: 'app-body-metrics-page',
  standalone: true,
  imports: [MeasuresPanel, BodyMetricsCharts],
  templateUrl: './body-metrics-page.html',
  styleUrl: './body-metrics-page.css',
})
export class BodyMetricsPage {
  readonly configs = MEASUREMENT_CONFIGS;
  selected = signal<BodyMetricsTab>('weights');

  select(tab: BodyMetricsTab) {
    this.selected.set(tab);
  }
}

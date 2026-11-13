import { Component, computed, input } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TrainingSetupStatus, VolumeLandmark } from '../../../data/training-api.service';
import {
  OnboardingStepKey, OnboardingStepState, baseTemplate, stepStates, stepUrl, STEP_LABELS, LANDMARK_ORDER,
} from '../onboarding-steps';

interface MinimapNode {
  key: OnboardingStepKey;
  label: string;
  state: OnboardingStepState;
  url: string | null;
}

/**
 * The map of the wizard: what the user designs (program, base template, sessions, volume levels) and what is
 * executed with it (the block). Each node fills in with what the user has created and links to its step
 */
@Component({
  selector: 'app-onboarding-minimap',
  standalone: true,
  imports: [RouterLink, NgTemplateOutlet],
  templateUrl: './onboarding-minimap.html',
})
export class OnboardingMinimap {
  status = input.required<TrainingSetupStatus>();
  current = input<OnboardingStepKey | null>(null);

  private nodes = computed<Record<OnboardingStepKey, MinimapNode>>(() => {
    const status = this.status();
    const states = stepStates(status, this.current());
    const node = (key: OnboardingStepKey): MinimapNode => ({
      key,
      label: STEP_LABELS[key],
      state: states[key],
      // The page being shown is not a link
      url: states[key] === 'current' ? null : stepUrl(key, status),
    });
    return {
      program: node('program'),
      template: node('template'),
      sessions: node('sessions'),
      levels: node('levels'),
      launch: node('launch'),
    };
  });

  design = computed(() => [this.nodes().program, this.nodes().template, this.nodes().sessions, this.nodes().levels]);
  execution = computed(() => [this.nodes().launch]);

  base = computed(() => baseTemplate(this.status()));
  /** Landmarks of the program's templates, from less to more volume */
  landmarks = computed(() => {
    const present = new Set(this.status().templates.map(t => t.volumeLandmark));
    return LANDMARK_ORDER.filter(l => present.has(l));
  });

  /** Colour of each landmark: it heats up with the volume, MV (deload) is the cool one */
  landmarkClass(landmark: VolumeLandmark): string {
    return {
      MV: 'bg-info/15 text-info',
      MEV: 'bg-accent/15 text-accent',
      MAV: 'bg-warning/15 text-warning',
      MRV: 'bg-danger/15 text-danger',
    }[landmark];
  }
}

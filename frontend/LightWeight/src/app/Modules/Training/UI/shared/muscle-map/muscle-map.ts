import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { MuscleGroup } from '../../../data/training-api.service';
import { MUSCLE_GROUPS } from '../muscle-groups';
import { BODY_REGIONS, MUSCLE_MAP_VIEWBOX } from './muscle-map-paths';

/** How a region of the body is painted */
export type RegionRole = 'primary' | 'secondary' | 'muscle' | 'body';

/**
 * Muscle group drawn by each region of the body map, by the prefix of the region id.
 * Regions that match none (head, hands, knees, serratus, tibialis...) are body, not a group
 */
const REGION_GROUPS: readonly (readonly [prefix: string, group: MuscleGroup])[] = [
  ['chest', 'Chest'],
  ['lats', 'Back'],
  ['lower-back', 'Back'],
  ['traps', 'Traps'],
  ['shoulder-front', 'FrontDelts'],
  ['shoulder-side', 'SideDelts'],
  ['deltoid-rear', 'RearDelts'],
  ['triceps', 'Triceps'],
  ['biceps', 'Biceps'],
  ['forearm', 'Forearms'],
  ['abs', 'Abs'],
  ['obliques', 'Abs'],
  ['gluteus', 'Glutes'],
  ['quads', 'Quads'],
  ['hamstrings', 'Hamstring'],
  ['calves', 'Calves'],
  ['adductors', 'Adductors'],
];

/** Muscle group of a region of the body map (null: it is not a muscle of the catalog) */
export function regionGroup(regionId: string): MuscleGroup | null {
  return REGION_GROUPS.find(([prefix]) => regionId.startsWith(prefix))?.[1] ?? null;
}

/** Role of every region, from the numeric values of the primary and secondary groups */
export function regionRoles(primary: readonly number[], secondary: readonly number[]): RegionRole[] {
  const primaryGroups = new Set(primary.map(group => MUSCLE_GROUPS[group]));
  const secondaryGroups = new Set(secondary.map(group => MUSCLE_GROUPS[group]));
  return BODY_REGIONS.map(region => {
    const group = regionGroup(region.id);
    if (!group) return 'body';
    if (primaryGroups.has(group)) return 'primary';
    if (secondaryGroups.has(group)) return 'secondary';
    return 'muscle';
  });
}

/**
 * Front and back silhouette with the muscle groups an exercise targets painted: the primary ones in
 * the accent, the secondary ones in a dimmed accent. Decorative by default (the groups are usually
 * written next to it); give it a `label` to announce it
 */
@Component({
  selector: 'app-muscle-map',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  /** Fills the box it is given; the viewBox keeps the proportions of the body */
  host: { class: 'block h-full w-full' },
  template: `
    <svg
      [attr.viewBox]="viewBox"
      class="block h-full w-full"
      [attr.role]="label() ? 'img' : null"
      [attr.aria-label]="label() || null"
      [attr.aria-hidden]="label() ? null : 'true'"
    >
      @for (region of regions(); track $index) {
        <path
          [attr.d]="region.d"
          class="stroke-canvas"
          [class.fill-accent]="region.role === 'primary'"
          [class.fill-accent/40]="region.role === 'secondary'"
          [class.fill-line]="region.role === 'muscle'"
          [class.fill-raised]="region.role === 'body'"
          stroke-width="0.25"
          stroke-linejoin="round"
        />
      }
    </svg>
  `,
})
export class MuscleMap {
  /** Numeric values of the groups it mainly targets */
  primary = input<readonly number[]>([]);
  /** Numeric values of the other groups it targets (primary ones are ignored here) */
  secondary = input<readonly number[]>([]);
  /** Accessible description; without it the map is hidden from screen readers */
  label = input('');

  protected readonly viewBox = MUSCLE_MAP_VIEWBOX;

  protected regions = computed(() => {
    const roles = regionRoles(this.primary(), this.secondary());
    return BODY_REGIONS.map((region, index) => ({ d: region.d, role: roles[index] }));
  });
}

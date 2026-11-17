import { MUSCLE_GROUPS } from '../muscle-groups';
import { BODY_REGIONS } from './muscle-map-paths';
import { regionGroup, regionRoles } from './muscle-map';

const value = (group: string) => (MUSCLE_GROUPS as string[]).indexOf(group);

describe('regionGroup', () => {
  it('draws every muscle group of the catalog in at least one region', () => {
    const drawn = new Set(BODY_REGIONS.map(region => regionGroup(region.id)));

    for (const group of MUSCLE_GROUPS) expect(drawn).toContain(group);
  });

  it('keeps the three heads of the deltoid apart', () => {
    expect(regionGroup('shoulder-front')).toBe('FrontDelts');
    expect(regionGroup('shoulder-side')).toBe('SideDelts');
    expect(regionGroup('deltoid-rear')).toBe('RearDelts');
  });

  it('treats the parts that are not a group as body', () => {
    expect(regionGroup('head')).toBeNull();
    expect(regionGroup('serratus-anterior')).toBeNull();
    expect(regionGroup('tibialis-anterior')).toBeNull();
  });
});

describe('regionRoles', () => {
  const rolesOf = (primary: number[], secondary: number[], regionId: string) => {
    const roles = regionRoles(primary, secondary);
    return BODY_REGIONS.flatMap((region, index) => (region.id === regionId ? [roles[index]] : []));
  };

  it('paints the primary and secondary groups and leaves the rest as muscle', () => {
    const primary = [value('Chest')];
    const secondary = [value('Triceps'), value('FrontDelts')];

    expect(rolesOf(primary, secondary, 'chest-upper')).toEqual(['primary', 'primary']);
    expect(rolesOf(primary, secondary, 'triceps-long')).toEqual(['secondary', 'secondary']);
    expect(rolesOf(primary, secondary, 'quads')).toEqual(['muscle', 'muscle']);
    expect(rolesOf(primary, secondary, 'head')).toEqual(['body']);
  });

  it('gives priority to primary when a group is in both lists', () => {
    expect(rolesOf([value('Back')], [value('Back')], 'lats-mid')).toEqual(['primary', 'primary']);
  });
});

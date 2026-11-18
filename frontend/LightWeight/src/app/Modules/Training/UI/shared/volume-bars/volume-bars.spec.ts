import { TestBed } from '@angular/core/testing';
import { VolumeBars, volumeBars } from './volume-bars';

describe('volumeBars', () => {
  it('lists the groups with sets from most to fewest, with their Spanish names', () => {
    const bars = volumeBars({ Chest: 8, Back: 10, Calves: 0, Biceps: 6 });

    expect(bars.map(b => b.group)).toEqual(['Back', 'Chest', 'Biceps']);
    expect(bars[0].label).toBe('Espalda');
    expect(bars.every(b => b.done === null)).toBe(true);
  });

  it('adds the sets done per group, never more than the planned ones', () => {
    const bars = volumeBars({ Back: 10, Biceps: 6, Traps: 2 }, { Back: 6, Biceps: 9 });

    expect(bars.map(b => [b.group, b.done])).toEqual([['Back', 6], ['Biceps', 6], ['Traps', 0]]);
  });
});

describe('VolumeBars', () => {
  async function render(volume: Record<string, number>) {
    await TestBed.configureTestingModule({ imports: [VolumeBars] }).compileComponents();
    const fixture = TestBed.createComponent(VolumeBars);
    fixture.componentRef.setInput('volume', volume);
    await fixture.whenStable();
    return { fixture, el: fixture.nativeElement as HTMLElement };
  }

  it('shows the first four groups and unfolds the rest', async () => {
    const { fixture, el } = await render({ Quads: 8, Hamstring: 6, Glutes: 4, Calves: 4, Abs: 3 });

    expect(el.querySelectorAll('li')).toHaveLength(4);
    const toggle = el.querySelector('button')!;
    expect(toggle.textContent).toContain('+1 grupo más');

    toggle.click();
    await fixture.whenStable();
    expect(el.querySelectorAll('li')).toHaveLength(5);
  });

  it('says so when there are no sets', async () => {
    const { el } = await render({});

    expect(el.textContent).toContain('Sin series todavía');
  });
});

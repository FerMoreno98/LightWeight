import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { TrainingTemplate } from '../../../data/training-api.service';
import { TemplateCard } from './template-card';

const template: TrainingTemplate = {
  id: 't1',
  name: 'Semana base',
  programId: 'p1',
  programName: 'Hipertrofia',
  order: 0,
  durationInDays: 7,
  volumeLandmark: 'MEV',
  trainingDistribution: 'UpperLower',
  totalVolume: { Chest: 12, Back: 14, Quads: 10, SideDelts: 10, Biceps: 6, Calves: 0 },
};

async function render(value: TrainingTemplate = template) {
  await TestBed.configureTestingModule({
    imports: [TemplateCard],
    providers: [provideRouter([])],
  }).compileComponents();
  const fixture = TestBed.createComponent(TemplateCard);
  fixture.componentRef.setInput('template', value);
  await fixture.whenStable();
  return { fixture, card: fixture.componentInstance, el: fixture.nativeElement as HTMLElement };
}

const button = (el: HTMLElement, text: string) =>
  [...el.querySelectorAll('button')].find(b => b.textContent?.trim().startsWith(text))!;

describe('TemplateCard', () => {
  it('adds up the weekly sets, leaving out the empty groups', async () => {
    const { card } = await render();

    expect(card.totalSets()).toBe(52);
  });

  it('says "series en N días" when the microcycle is not a week', async () => {
    const { card } = await render({ ...template, durationInDays: 10 });

    expect(card.totalLabel()).toBe('series en 10 días');
  });

  it('keeps the level picker open after choosing "Cambiar nivel" in the menu', async () => {
    const { fixture, card, el } = await render();
    button(el, '').click(); // the ⋯ button is the first one
    await fixture.whenStable();

    button(el, 'Cambiar nivel').click();
    await fixture.whenStable();

    expect(card.editingLandmark()).toBe(true);
    expect(el.querySelectorAll('fieldset button')).toHaveLength(4);
  });

  it('emits the new level and closes the picker, and ignores choosing the same one', async () => {
    const { fixture, card } = await render();
    const emitted: string[] = [];
    card.landmarkChange.subscribe(landmark => emitted.push(landmark));

    card.startLandmarkEdit();
    card.chooseLandmark('MEV');
    card.startLandmarkEdit();
    card.chooseLandmark('MAV');
    await fixture.whenStable();

    expect(emitted).toEqual(['MAV']);
    expect(card.editingLandmark()).toBe(false);
  });

  it('closes the menu when clicking outside the card', async () => {
    const { fixture, card } = await render();
    card.toggleMenu();

    document.body.click();
    await fixture.whenStable();

    expect(card.menuOpen()).toBe(false);
  });
});

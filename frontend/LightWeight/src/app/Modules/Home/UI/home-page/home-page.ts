import { Component } from '@angular/core';
import { CurrentWeek } from '../../../Training/UI/current-week/current-week';
import { TrainingOverview } from '../../../Dashboard/UI/training-overview/training-overview';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CurrentWeek, TrainingOverview],
  templateUrl: './home-page.html',
  styleUrl: './home-page.css',
})
export class HomePage {

}

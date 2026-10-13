import { Component } from '@angular/core';
import { CurrentWeek } from '../../../Training/UI/current-week/current-week';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [CurrentWeek],
  templateUrl: './home-page.html',
  styleUrl: './home-page.css',
})
export class HomePage {

}

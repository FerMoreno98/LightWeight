import { Component, input, OnInit, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProfileData, Sex } from '../../data/user-profile-api.service';

/** YYYY-MM-DD of a date in local time, the format of an input type="date" */
function isoDay(date: Date): string {
    const pad = (n: number) => String(n).padStart(2, '0');
    return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

/**
 * Name, sex and date of birth. The same form for the onboarding and for editing the profile;
 * the limits are the ones of the backend (name up to 50 characters, born in the last 120 years)
 */
@Component({
    selector: 'app-profile-form',
    standalone: true,
    imports: [FormsModule],
    templateUrl: './profile-form.html',
})
export class ProfileForm implements OnInit {
    /** Values to start from; null for an empty form */
    initial = input<ProfileData | null>(null);
    submitLabel = input.required<string>();
    busyLabel = input.required<string>();
    busy = input(false);
    error = input<string | null>(null);

    save = output<ProfileData>();

    readonly nameMaxLength = 50;
    readonly sexOptions: { value: Sex; label: string }[] = [
        { value: 'Female', label: 'Mujer' },
        { value: 'Male', label: 'Hombre' },
    ];
    readonly maxDate = isoDay(new Date());
    readonly minDate = (() => {
        const date = new Date();
        date.setFullYear(date.getFullYear() - 120);
        date.setDate(date.getDate() + 1);
        return isoDay(date);
    })();

    name = '';
    sex: Sex | '' = '';
    dateOfBirth = '';

    ngOnInit() {
        const initial = this.initial();
        if (initial) {
            this.name = initial.name;
            this.sex = initial.sex;
            this.dateOfBirth = initial.dateOfBirth;
        }
    }

    onSubmit() {
        if (!this.sex) return;
        this.save.emit({ name: this.name.trim(), sex: this.sex, dateOfBirth: this.dateOfBirth });
    }
}

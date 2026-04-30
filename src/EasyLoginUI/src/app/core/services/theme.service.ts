import { Injectable, effect, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly storageKey = 'dark-mode';

  isDarkMode = signal<boolean>(localStorage.getItem(this.storageKey) === 'true');

  constructor() {
    document.body.classList.toggle('dark-mode', this.isDarkMode());

    effect(() => {
      const dark = this.isDarkMode();
      document.body.classList.toggle('dark-mode', dark);
      localStorage.setItem(this.storageKey, String(dark));
    });
  }

  toggle(): void {
    this.isDarkMode.update(v => !v);
  }
}

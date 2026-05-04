import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WorkerControlsComponent } from './components/worker-controls/worker-controls.component';
import { SignalrService } from './services/signalr.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, WorkerControlsComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  protected readonly title = 'hireops-ui';
  protected readonly signalr = inject(SignalrService);

  // 🔹 Замени на порт, который выводит `dotnet run` (обычно 7001 или 5001)
  private readonly apiUrl = '';

  ngOnInit() {

    this.signalr.connect(this.apiUrl);
  }

  ngOnDestroy() {
    this.signalr.disconnect();
  }

  async startWave(count: number) {
    try {
      // 🔹 ВАЖНО: относительный путь (начинается с /), чтобы сработал прокси
      const response = await fetch(`/api/simulations/wave?applicantCount=${count}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      console.log(`✅ Wave started: ${count} applicants`);
    } catch (err) {
      console.error('❌ Failed to start wave:', err);
    }
  }
}

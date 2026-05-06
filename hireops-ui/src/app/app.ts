import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TeamControlsComponent } from './components/worker-controls/worker-controls.component';
import { SignalrService } from './services/signalr.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule, TeamControlsComponent],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App implements OnInit, OnDestroy {
  protected readonly title = 'hireops-ui';
  protected readonly signalr = inject(SignalrService);
  private readonly apiUrl = ''; // Прокси

  // 🔹 Сигнал для блокировки кнопок волны
  protected isProcessing = signal(false);

  // 🔹 Сигнал для режима хаоса (стресс-тест)
  protected chaosMode = signal(false);

  // 🔹 Таймер авто-разблокировки (защита от зависаний)
  private processingTimeout?: ReturnType<typeof setTimeout>;

  ngOnInit() {
    this.signalr.connect(this.apiUrl);
  }

  ngOnDestroy() {
    this.signalr.disconnect();
    if (this.processingTimeout) clearTimeout(this.processingTimeout);
  }

  async startWave(count: number) {
    // 🔹 Блокируем кнопки сразу
    this.isProcessing.set(true);

    // 🔹 Страховка: если ответ не придёт за 30с — разблокируем
    this.processingTimeout = setTimeout(() => {
      this.isProcessing.set(false);
      console.warn('⚠️ Processing timeout, buttons unlocked');
    }, 30000);

    try {
      const response = await fetch(`/api/simulations/wave?applicantCount=${count}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      console.log(`✅ Wave started: ${count} applicants`);
      this.isProcessing.set(false);
      if (this.processingTimeout) clearTimeout(this.processingTimeout);

    } catch (err) {
      console.error('❌ Failed to start wave:', err);
      this.isProcessing.set(false);
      if (this.processingTimeout) clearTimeout(this.processingTimeout);
    }
  }

  // 🔹 НОВЫЙ МЕТОД: Переключение режима хаоса
  async toggleChaosMode() {
    try {
      const response = await fetch('/api/simulations/chaos/toggle', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}: ${response.statusText}`);
      }

      const result = await response.json();
      // 🔹 Обновляем локальное состояние на основе ответа сервера
      this.chaosMode.set(result.enabled);
      console.log(result.message);

    } catch (err) {
      console.error('❌ Failed to toggle chaos mode:', err);
      // В случае ошибки — инвертируем локальное состояние для визуального отката
      this.chaosMode.update(v => !v);
    }
  }
}

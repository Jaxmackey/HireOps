import { Component, inject, signal, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TeamControlsComponent } from './components/worker-controls/worker-controls.component';
import { SignalrService } from './services/signalr.service';
import { TenantService } from './services/tenant.service';

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
  protected readonly tenant = inject(TenantService);
  private readonly apiUrl = ''; // Прокси

  // 🔹 Сигнал для блокировки кнопок волны
  protected isProcessing = signal(false);

  // 🔹 Сигнал для режима хаоса (стресс-тест)
  protected chaosMode = signal(false);

  // 🔹 Таймер авто-разблокировки (защита от зависаний)
  private processingTimeout?: ReturnType<typeof setTimeout>;

  ngOnInit() {
    this.tenant.initFromStorage();
    var t = this.tenant.currentTenantId();
    if (!t) {
      const demoTenant = crypto.randomUUID();
      this.tenant.setTenant(demoTenant);
      console.log('🎭 Demo tenant assigned:', demoTenant);
    }
    this.signalr.connect(this.apiUrl);
  }

  ngOnDestroy() {
    this.signalr.disconnect();
    if (this.processingTimeout) clearTimeout(this.processingTimeout);
  }

  async startWave(count: number) {
    // 🔹 ВАЛИДАЦИЯ: проверяем, что на всех этапах есть хотя бы 1 рекрутер
    const workers = this.signalr.workers();
    const requiredStages = ['sim.received', 'sim.screening', 'sim.tech'];
    const emptyStages = requiredStages.filter(stage => (workers?.[stage] ?? 0) === 0);

    if (emptyStages.length > 0) {
      alert(`❌ Нельзя запустить волну!\nНа следующих этапах нет рекрутеров:\n• ${emptyStages.join('\n• ')}`);
      return;
    }

    // ✅ Валидация пройдена — блокируем и запускаем
    this.isProcessing.set(true);
    // 🔹 Сбрасываем старый прогресс и ставим начальный
    this.signalr.waveProgress.set({ total: count, processed: 0, percent: 0 });

    try {
      const response = await fetch(`/api/simulations/wave?applicantCount=${count}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      });

      if (!response.ok) {
        const error = await response.json().catch(() => ({}));
        if (error.emptyStages) {
          alert(`❌ ${error.message}`);
        } else {
          throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        // При ошибке разблокируем сразу
        this.isProcessing.set(false);
        this.signalr.waveProgress.set(null);
        return;
      }

      console.log(`✅ Wave started: ${count} applicants`);

      // ❗ ВАЖНО: НЕ разблокируем кнопки здесь!
      // Ждём события waveCompleted от сервера.

    } catch (err) {
      console.error('❌ Failed to start wave:', err);
      this.isProcessing.set(false);
      this.signalr.waveProgress.set(null);
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

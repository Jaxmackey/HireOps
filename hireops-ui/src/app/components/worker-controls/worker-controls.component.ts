import { Component, inject, signal, effect, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SignalrService } from '../../services/signalr.service';

@Component({
  selector: 'app-team-controls',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bg-gray-800 p-4 rounded-lg border border-gray-700">
      <h3 class="text-lg font-semibold mb-4 text-white">👥 Управление командой</h3>

      <div class="mb-4">
        <label class="block text-sm text-gray-400 mb-1">Этап отбора</label>
        <select
          [(ngModel)]="selectedStage"
          (ngModelChange)="onStageChange()"
          class="w-full bg-gray-700 border border-gray-600 rounded px-3 py-2 text-white">
          <option value="sim.received">📥 Входящие отклики</option>
          <option value="sim.screening">🔍 Первичный скрининг</option>
          <option value="sim.tech">💻 Техническое интервью</option>
        </select>
      </div>

      <div class="mb-4">
        <div class="flex justify-between text-sm text-gray-400 mb-1">
          <span>Рекрутеров на этапе</span>
          <span class="font-mono text-green-400">{{ agentCount }}</span>
        </div>
        <div class="flex gap-2">
          <button (click)="removeAgent()" [disabled]="agentCount <= 0"
            class="flex-1 bg-red-600 hover:bg-red-700 disabled:bg-gray-600 px-3 py-2 rounded text-sm text-white transition">
            ➖ Убрать
          </button>
          <button (click)="addAgent()"
            class="flex-1 bg-green-600 hover:bg-green-700 px-3 py-2 rounded text-sm text-white transition">
            ➕ Нанять
          </button>
        </div>
      </div>

      <div>
        <div class="flex justify-between text-sm text-gray-400 mb-1">
          <span>Задач на рекрутера</span>
          <span class="font-mono text-blue-400">{{ currentBatchSize }}</span>
        </div>
        <input type="range" min="1" max="100" [ngModel]="batchSizeValue()" (change)="updateBatchSize($event)"
          class="w-full accent-blue-500">
        <p class="text-xs text-gray-500 mt-1">💡 Меньше = тщательнее разбор, больше = выше скорость</p>
      </div>
    </div>
  `
})
export class TeamControlsComponent implements OnInit {
  private signalr = inject(SignalrService);

  selectedStage = 'sim.received';
  batchSizeValue = signal(10);
  localWorkerCount = signal(0);

  constructor() {
    // 🔹 Реактивная подписка на изменения с сервера
    effect(() => {
      const workers = this.signalr.workers();
      // Обновляем локальный счетчик ТОЛЬКО на основе данных с сервера
      const count = workers?.[this.selectedStage];
      if (count !== undefined) {
        this.localWorkerCount.set(count);
      }
    });
  }

  ngOnInit() {
    // Инициализация при загрузке (на случай, если данные уже есть)
    const workers = this.signalr.workers();
    const count = workers?.[this.selectedStage];
    if (count !== undefined) {
      this.localWorkerCount.set(count);
    }
  }

  get agentCount(): number {
    return this.localWorkerCount();
  }

  get currentBatchSize(): number {
    return this.signalr.prefetch();
  }

  onStageChange() {
    // При смене этапа просто перечитываем значение из сигнала (effect сработает сам)
    const workers = this.signalr.workers();
    const count = workers?.[this.selectedStage];
    if (count !== undefined) {
      this.localWorkerCount.set(count);
    }
  }

  // 🔹 ИСПРАВЛЕНО: Убрали оптимистичное обновление. Ждем ответа сервера.
  async addAgent() {
    try {
      await this.signalr.addWorker(this.selectedStage);
      // Сервер сам пришлет WorkersUpdated -> effect обновит localWorkerCount
    } catch (err) {
      console.error('Failed to add agent:', err);
    }
  }

  async removeAgent() {
    try {
      await this.signalr.removeWorker(this.selectedStage);
      // Сервер сам пришлет WorkersUpdated -> effect обновит localWorkerCount
    } catch (err) {
      console.error('Failed to remove agent:', err);
    }
  }

  async updateBatchSize(event: Event) {
    const value = (event.target as HTMLInputElement).valueAsNumber;
    await this.signalr.updatePrefetch(value);
  }
}

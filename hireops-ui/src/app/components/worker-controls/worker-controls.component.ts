import { Component, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SignalrService } from '../../services/signalr.service';

@Component({
  selector: 'app-team-controls', // 👈 Переименовали селектор
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bg-gray-800 p-4 rounded-lg border border-gray-700">
      <h3 class="text-lg font-semibold mb-4 text-white">👥 Управление командой</h3>

      <!-- Этап воронки -->
      <div class="mb-4">
        <label class="block text-sm text-gray-400 mb-1">Этап отбора</label>
        <select
          [(ngModel)]="selectedStage"
          class="w-full bg-gray-700 border border-gray-600 rounded px-3 py-2 text-white"
          title="Стадия воронки найма">
          <option value="sim.received">📥 Входящие отклики</option>
          <option value="sim.screening">🔍 Первичный скрининг</option>
          <option value="sim.tech">💻 Техническое интервью</option>
          <option value="sim.hr">🤝 Финальное решение</option>
        </select>
      </div>

      <!-- Размер команды -->
      <div class="mb-4">
        <div class="flex justify-between text-sm text-gray-400 mb-1">
          <span>Рекрутеров на этапе</span>
          <span class="font-mono text-green-400" title="Активные обработчики сообщений">
            {{ agentCount }}
          </span>
        </div>
        <div class="flex gap-2">
          <button (click)="removeAgent()" [disabled]="agentCount <= 1"
            class="flex-1 bg-red-600 hover:bg-red-700 disabled:bg-gray-600 px-3 py-2 rounded text-sm text-white transition"
            title="Уменьшить команду (освободить ресурс)">
            ➖ Убрать
          </button>
          <button (click)="addAgent()"
            class="flex-1 bg-green-600 hover:bg-green-700 px-3 py-2 rounded text-sm text-white transition"
            title="Добавить рекрутера (увеличить пропускную способность)">
            ➕ Нанять
          </button>
        </div>
      </div>

      <!-- Нагрузка на человека -->
      <div>
        <div class="flex justify-between text-sm text-gray-400 mb-1">
          <span>Задач на рекрутера</span>
          <span class="font-mono text-blue-400" title="Prefetch: сколько резюме берёт в работу один человек">
            {{ currentBatchSize }}
          </span>
        </div>
        <input type="range" min="1" max="100" [ngModel]="batchSizeValue()" (change)="updateBatchSize($event)"
          class="w-full accent-blue-500"
          title="Баланс: больше задач → выше скорость, но выше риск перегрузки">
        <p class="text-xs text-gray-500 mt-1">
          💡 Меньше = тщательнее разбор, больше = выше скорость
        </p>
      </div>
    </div>
  `
})
export class TeamControlsComponent {
  private signalr = inject(SignalrService);

  // 🔹 HR-названия переменных (внутри — технические ключи)
  selectedStage = 'sim.received';
  batchSizeValue = signal(10);

  constructor() {
    effect(() => {
      this.batchSizeValue.set(this.signalr.prefetch());
    });
  }

  // 🔹 Геттеры с переводом технических метрик
  get agentCount(): number {
    return this.signalr.workers()[this.selectedStage] ?? 1;
  }

  get currentBatchSize(): number {
    return this.signalr.prefetch();
  }

  // 🔹 Методы с понятными названиями (внутри — технические вызовы)
  async addAgent() {
    await this.signalr.addWorker(this.selectedStage);
  }

  async removeAgent() {
    await this.signalr.removeWorker(this.selectedStage);
  }

  async updateBatchSize(event: Event) {
    const value = (event.target as HTMLInputElement).valueAsNumber;
    await this.signalr.updatePrefetch(value);
  }
}

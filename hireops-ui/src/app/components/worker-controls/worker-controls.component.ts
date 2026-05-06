import { Component, inject, signal, effect, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SignalrService } from '../../services/signalr.service';
import { TenantService } from '../../services/tenant.service';

// 🔹 Тип для этапа (удобно для итерации)
interface Stage {
  key: string;
  label: string;
  icon: string;
}

@Component({
  selector: 'app-team-controls',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bg-gray-800 p-4 rounded-lg border border-gray-700">
      <h3 class="text-lg font-semibold mb-4 text-white">👥 Управление командой</h3>

      <!-- Выбор этапа -->
      <div class="mb-4">
        <label class="block text-sm text-gray-400 mb-1">Этап отбора</label>
        <select
          [(ngModel)]="selectedStageSignal"
          class="w-full bg-gray-700 border border-gray-600 rounded px-3 py-2 text-white">
          @for (stage of stages; track stage.key) {
            <option [value]="stage.key">{{ stage.icon }} {{ stage.label }}</option>
          }
        </select>
      </div>

      <!-- Управление для выбранного этапа -->
      <div class="mb-4">
        <div class="flex justify-between text-sm text-gray-400 mb-1">
          <span>Рекрутеров на этапе </span>
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

      <!-- 🔹 НОВАЯ: Сводная таблица по всем этапам -->
      <div class="border-t border-gray-700 pt-4">
        <h4 class="text-sm font-semibold text-gray-300 mb-2">📊 Команда по этапам</h4>
        <div class="space-y-2">
          @for (stage of stages; track stage.key) {
            <div class="flex items-center justify-between text-sm">
              <span class="text-gray-400">{{ stage.icon }} {{ stage.label }}</span>
              <span
                class="font-mono px-2 py-0.5 rounded"
                [class.text-green-400]="getWorkerCount(stage.key) > 0"
                [class.text-gray-500]="getWorkerCount(stage.key) === 0">
                {{ getWorkerCount(stage.key) }}
              </span>
            </div>
          }
        </div>
      </div>
    </div>
  `
})
export class TeamControlsComponent implements OnInit {
  private signalr = inject(SignalrService);
  private tenant = inject(TenantService);
  // 🔹 Список этапов (константа)
  readonly stages: Stage[] = [
    { key: 'sim.received', label: 'Входящие отклики', icon: '📥' },
    { key: 'sim.screening', label: 'Первичный скрининг', icon: '🔍' },
    { key: 'sim.tech', label: 'Техническое интервью', icon: '💻' }
  ];

  selectedStageSignal = signal('sim.received');
  localWorkerCount = signal(0);

  constructor() {
    effect(() => {
      const workers = this.signalr.workers();
      const stage = this.selectedStageSignal();
      const count = workers?.[stage] ?? 0;
      this.localWorkerCount.set(count);
    });
  }

  ngOnInit() {
    const workers = this.signalr.workers();
    const stage = this.selectedStageSignal();
    const count = workers?.[stage] ?? 0;
    this.localWorkerCount.set(count);
  }

  get agentCount(): number {
    return this.localWorkerCount();
  }

  // 🔹 Helper: получить количество воркеров для любого этапа
  getWorkerCount(stageKey: string): number {
    return this.signalr.workers()?.[stageKey] ?? 0;
  }

  async addAgent() {
    try {
      // 🔹 Передаём tenantId, если он установлен
      await this.signalr.addWorker(
        this.selectedStageSignal(),
        this.tenant.currentTenantId() ?? undefined
      );
    } catch (err) {
      console.error('Failed to add agent:', err);
    }
  }

  async removeAgent() {
    try {
      await this.signalr.removeWorker(
        this.selectedStageSignal(),
        this.tenant.currentTenantId() ?? undefined
      );
    } catch (err) {
      console.error('Failed to remove agent:', err);
    }
  }
}

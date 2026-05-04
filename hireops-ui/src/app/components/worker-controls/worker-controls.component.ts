import { Component, inject, signal, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SignalrService } from '../../services/signalr.service';

@Component({
  selector: 'app-worker-controls',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="bg-gray-800 p-4 rounded-lg border border-gray-700">
      <h3 class="text-lg font-semibold mb-4 text-white">⚙️ Worker Controls</h3>

      <div class="mb-4">
        <label class="block text-sm text-gray-400 mb-1">Queue</label>
        <select
          [(ngModel)]="selectedQueue"
          class="w-full bg-gray-700 border border-gray-600 rounded px-3 py-2 text-white">
          <option value="sim.received">sim.received</option>
          <option value="sim.screening">sim.screening</option>
          <option value="sim.tech">sim.tech</option>
        </select>
      </div>

      <div class="mb-4">
        <div class="flex justify-between text-sm text-gray-400 mb-1">
          <span>Active Workers</span>
          <span class="font-mono text-green-400">{{ workerCount }}</span>
        </div>
        <div class="flex gap-2">
          <button (click)="removeWorker()" [disabled]="workerCount <= 0"
            class="flex-1 bg-red-600 hover:bg-red-700 disabled:bg-gray-600 px-3 py-2 rounded text-sm text-white transition">
            ➖ Remove
          </button>
          <button (click)="addWorker()"
            class="flex-1 bg-green-600 hover:bg-green-700 px-3 py-2 rounded text-sm text-white transition">
            ➕ Add
          </button>
        </div>
      </div>

      <div>
        <div class="flex justify-between text-sm text-gray-400 mb-1">
          <span>Prefetch Count</span>
          <span class="font-mono text-blue-400">{{ currentPrefetch }}</span>
        </div>
        <input type="range" min="1" max="100" [ngModel]="prefetchValue()" (change)="updatePrefetch($event)"
          class="w-full accent-blue-500">
      </div>
    </div>
  `
})
export class WorkerControlsComponent {
  private signalr = inject(SignalrService);

  selectedQueue = 'sim.received';
  prefetchValue = signal(10);

  constructor() {
    // 🔹 React to SignalR updates automatically
    effect(() => {
      this.prefetchValue.set(this.signalr.prefetch());
    });
  }

  // 🔹 Геттеры автоматически пересчитываются при изменении сигналов
  get workerCount(): number {
    return this.signalr.workers()[this.selectedQueue] ?? 1;
  }

  get currentPrefetch(): number {
    return this.signalr.prefetch();
  }

  async addWorker() {
    await this.signalr.addWorker(this.selectedQueue);
  }

  async removeWorker() {
    await this.signalr.removeWorker(this.selectedQueue);
  }

  updatePrefetch(event: Event) {
    const value = (event.target as HTMLInputElement).valueAsNumber;
    this.signalr.updatePrefetch(value);
  }
}

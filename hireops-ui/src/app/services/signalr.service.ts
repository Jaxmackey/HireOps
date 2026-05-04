import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

export interface Metrics {
  queueDepth: number;
  latency: number;
  processedPerSec: number;
  processedTotal: number;
  workers: Record<string, number>;
  throughput: Record<string, number>;
}

@Injectable({ providedIn: 'root' })
export class SignalrService {
  public hubConnection?: signalR.HubConnection;

  // Сигналы для реактивного UI
  public metrics = signal<Metrics | null>(null);
  public workers = signal<Record<string, number>>({});
  public prefetch = signal(10);

  // 🔹 Инициализация с авто-реконнектом
  connect(baseUrl: string) {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(`/hubs/dashboard`, {
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000]) // Переподключение с экспоненциальной задержкой
      .configureLogging(signalR.LogLevel.Information)
      .build();

    // 🔹 Подписка на метрики
    this.hubConnection.on('MetricsUpdate', (data: Metrics) => {
      this.metrics.set(data);
      if (data.workers) this.workers.set(data.workers);
    });

    // 🔹 Подписка на обновления воркеров
    this.hubConnection.on('WorkersUpdated', (stats: Record<string, number>) => {
      this.workers.set(stats);
    });

    // 🔹 Подписка на изменения prefetch
    this.hubConnection.on('PrefetchUpdated', (value: number) => {
      this.prefetch.set(value);
    });

    // 🔹 Запуск соединения
    this.hubConnection.start()
      .then(() => console.log('✅ SignalR connected'))
      .catch(err => console.error('❌ SignalR error:', err));
  }

  // 🔹 Методы для управления воркерами
  addWorker(queue: string) {
    return this.hubConnection?.invoke('AddWorker', queue);
  }

  removeWorker(queue: string) {
    return this.hubConnection?.invoke('RemoveWorker', queue);
  }

  updatePrefetch(count: number) {
    return this.hubConnection?.invoke('UpdatePrefetch', count);
  }

  disconnect() {
    this.hubConnection?.stop();
  }
}

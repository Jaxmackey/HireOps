import { Injectable, signal } from '@angular/core';
import * as signalR from '@microsoft/signalr';

export interface Metrics {
  queueDepth: number;
  latency: number;
  processedPerSec: number;
  processedTotal: number;
  throughput: Record<string, number>;
}

@Injectable({ providedIn: 'root' })
export class SignalrService {
  public hubConnection?: signalR.HubConnection;

  // Сигналы для реактивного UI
  public metrics = signal<Metrics | null>(null);
  public workers = signal<Record<string, number>>({});

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
      console.log('📊 MetricsUpdate received (workers field ignored)');
      this.metrics.set(data);
    });

    // 🔹 Подписка на обновления воркеров
    this.hubConnection.on('WorkersUpdated', (stats: Record<string, number>) => {
      console.log('👥 WorkersUpdated received:', stats);
      this.workers.set(stats);
    });

    // 🔹 Запуск соединения
    this.hubConnection.start()
      .then(() => console.log('✅ SignalR connected'))
      .catch(err => console.error('❌ SignalR error:', err));
  }

  addWorker(queue: string, tenantId?: string) {
    return this.hubConnection?.invoke('AddWorker', queue, tenantId ? tenantId : null);
  }

  removeWorker(queue: string, tenantId?: string) {
    return this.hubConnection?.invoke('RemoveWorker', queue, tenantId ? tenantId : null);
  }

  disconnect() {
    this.hubConnection?.stop();
  }
}

import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TenantService {
  // 🔹 Текущий тенант (задаётся при входе в симуляцию)
  public currentTenantId = signal<string | null>(null);

  // 🔹 Установка тенанта (вызывается при входе/регистрации)
  setTenant(tenantId: string) {
    this.currentTenantId.set(tenantId);
    // Опционально: сохранить в localStorage для сохранения между перезагрузками
    localStorage.setItem('hireops_tenant_id', tenantId);
  }

  // 🔹 Инициализация из localStorage (при загрузке страницы)
  initFromStorage() {
    const stored = localStorage.getItem('hireops_tenant_id');
    if (stored) {
      this.currentTenantId.set(stored);
    }
  }

  // 🔹 Очистка (при выходе)
  clear() {
    this.currentTenantId.set(null);
    localStorage.removeItem('hireops_tenant_id');
  }
}

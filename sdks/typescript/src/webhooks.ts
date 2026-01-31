import { AxiosInstance } from 'axios';
import { Webhook, CreateWebhookRequest } from './types';

export class WebhooksService {
  constructor(private axios: AxiosInstance) {}

  async getAll(): Promise<Webhook[]> {
    const response = await this.axios.get<Webhook[]>('/webhooks');
    return response.data;
  }

  async getById(id: number): Promise<Webhook> {
    const response = await this.axios.get<Webhook>(`/webhooks/${id}`);
    return response.data;
  }

  async create(webhook: CreateWebhookRequest): Promise<Webhook> {
    const response = await this.axios.post<Webhook>('/webhooks', webhook);
    return response.data;
  }

  async update(id: number, webhook: Partial<CreateWebhookRequest>): Promise<Webhook> {
    const response = await this.axios.put<Webhook>(`/webhooks/${id}`, webhook);
    return response.data;
  }

  async delete(id: number): Promise<void> {
    await this.axios.delete(`/webhooks/${id}`);
  }
}

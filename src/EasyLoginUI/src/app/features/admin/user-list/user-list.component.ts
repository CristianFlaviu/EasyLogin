import { Component, inject, OnInit } from '@angular/core';
import { PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatCardModule } from '@angular/material/card';
import { ApiService } from '../../../core/services/api.service';
import { PaginatedList, UserListItem } from '../../../core/models/user.model';

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [
    MatTableModule, MatPaginatorModule, MatProgressBarModule,
    MatChipsModule, MatCardModule,
  ],
  templateUrl: './user-list.component.html',
  styleUrl: './user-list.component.scss',
})
export class UserListComponent implements OnInit {
  private readonly api = inject(ApiService);

  displayedColumns = ['name', 'email', 'roles', 'status'];
  users: UserListItem[] = [];
  totalCount = 0;
  pageSize = 20;
  pageIndex = 0;
  loading = false;

  ngOnInit(): void {
    this.loadUsers();
  }

  loadUsers(): void {
    this.loading = true;
    this.api.get<PaginatedList<UserListItem>>(
      `/admin/users?pageNumber=${this.pageIndex + 1}&pageSize=${this.pageSize}`
    ).subscribe({
      next: data => {
        this.users = data.items;
        this.totalCount = data.totalCount;
        this.loading = false;
      },
      error: () => { this.loading = false; },
    });
  }

  onPage(event: PageEvent): void {
    this.pageIndex = event.pageIndex;
    this.pageSize = event.pageSize;
    this.loadUsers();
  }
}

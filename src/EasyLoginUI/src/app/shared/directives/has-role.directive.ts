import { Directive, Input, TemplateRef, ViewContainerRef, OnInit, OnDestroy, inject } from '@angular/core';
import { Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';

@Directive({
  selector: '[appHasRole]',
  standalone: true,
})
export class HasRoleDirective implements OnInit, OnDestroy {
  private roles: string[] = [];
  private sub?: Subscription;

  private readonly auth = inject(AuthService);
  private readonly tmpl = inject(TemplateRef<unknown>);
  private readonly vc = inject(ViewContainerRef);

  @Input() set appHasRole(value: string | string[]) {
    this.roles = Array.isArray(value) ? value : [value];
    this.updateView();
  }

  ngOnInit(): void {
    this.sub = this.auth.currentUser$.subscribe(() => this.updateView());
  }

  ngOnDestroy(): void {
    this.sub?.unsubscribe();
  }

  private updateView(): void {
    this.vc.clear();
    if (this.roles.some(r => this.auth.hasRole(r))) {
      this.vc.createEmbeddedView(this.tmpl);
    }
  }
}

import { DndContext, closestCenter, type DragEndEvent } from '@dnd-kit/core';
import { SortableContext, useSortable, verticalListSortingStrategy } from '@dnd-kit/sortable';
import { CSS } from '@dnd-kit/utilities';
import { NavLink } from 'react-router-dom';
import { useDashboard } from '../hooks/useDashboard';
import type { DashboardPage } from '../types/api';

function SidebarItem({ page, onToggleFavorite }: { page: DashboardPage; onToggleFavorite: (id: string) => void }) {
  const { attributes, listeners, setNodeRef, transform, transition } = useSortable({ id: page.moduleId });
  const style = { transform: CSS.Transform.toString(transform), transition };

  return (
    <div ref={setNodeRef} style={style} className="flex items-center group">
      <button
        className="text-yellow-400 mr-1 text-xs opacity-60 hover:opacity-100 px-1"
        onClick={() => onToggleFavorite(page.moduleId)}
        title={page.isFavorite ? 'Remove favorite' : 'Add favorite'}
      >
        {page.isFavorite ? '★' : '☆'}
      </button>
      <NavLink
        to={`/mod/${page.moduleId}`}
        className={({ isActive }) =>
          `flex-1 px-3 py-2 rounded text-sm transition-colors ${
            isActive ? 'bg-blue-600 text-white' : 'text-gray-200 hover:bg-gray-700'
          }`
        }
      >
        {page.moduleId}
      </NavLink>
      <span
        {...attributes}
        {...listeners}
        className="cursor-grab px-1 text-gray-500 opacity-0 group-hover:opacity-100 select-none"
        title="Drag to reorder"
      >
        ⠿
      </span>
    </div>
  );
}

export default function Sidebar() {
  const { data: pages = [], setPages } = useDashboard();

  const sorted = [...pages].sort((a, b) => {
    if (a.isFavorite !== b.isFavorite) return a.isFavorite ? -1 : 1;
    return a.order - b.order;
  });

  function handleDragEnd(e: DragEndEvent) {
    const { active, over } = e;
    if (!over || active.id === over.id) return;
    const oldIndex = sorted.findIndex(p => p.moduleId === active.id);
    const newIndex = sorted.findIndex(p => p.moduleId === over.id);
    const reordered = [...sorted];
    reordered.splice(newIndex, 0, reordered.splice(oldIndex, 1)[0]);
    setPages.mutate(reordered.map((p, i) => ({ ...p, order: i })));
  }

  function toggleFavorite(moduleId: string) {
    const updated = pages.map(p =>
      p.moduleId === moduleId ? { ...p, isFavorite: !p.isFavorite } : p
    );
    setPages.mutate(updated);
  }

  return (
    <nav className="w-56 bg-gray-800 flex flex-col h-full p-2 gap-1 shrink-0">
      <div className="text-white font-bold px-3 py-2 text-lg tracking-tight">Codex</div>
      <DndContext collisionDetection={closestCenter} onDragEnd={handleDragEnd}>
        <SortableContext items={sorted.map(p => p.moduleId)} strategy={verticalListSortingStrategy}>
          {sorted.map(page => (
            <SidebarItem key={page.moduleId} page={page} onToggleFavorite={toggleFavorite} />
          ))}
        </SortableContext>
      </DndContext>
      <div className="mt-auto border-t border-gray-600 pt-2 flex flex-col gap-1">
        <NavLink
          to="/store"
          className={({ isActive }) =>
            `px-3 py-2 rounded text-sm transition-colors ${
              isActive ? 'bg-blue-600 text-white' : 'text-gray-300 hover:bg-gray-700'
            }`
          }
        >
          + Store
        </NavLink>
        <NavLink
          to="/datasources"
          className={({ isActive }) =>
            `px-3 py-2 rounded text-sm transition-colors ${
              isActive ? 'bg-blue-600 text-white' : 'text-gray-300 hover:bg-gray-700'
            }`
          }
        >
          + Sources
        </NavLink>
      </div>
    </nav>
  );
}

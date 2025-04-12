import React from "react";

interface FilterControlsProps {
  onSortChange: (sortBy: string) => void;
  sortBy: string;
}

const FilterControls: React.FC<FilterControlsProps> = ({ onSortChange, sortBy }) => {
  return (
    <div className="mb-6 flex items-center">
      <label htmlFor="sort-by" className="mr-2 text-gray-700">
        Sort by:
      </label>
      <select
        id="sort-by"
        value={sortBy}
        onChange={(e) => onSortChange(e.target.value)}
        className="rounded-md border border-gray-300 py-1 px-3 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
      >
        <option value="title">Title</option>
        <option value="year">Year</option>
        <option value="price-low">Price: Low to High</option>
        <option value="price-high">Price: High to Low</option>
      </select>
    </div>
  );
};

export default FilterControls; 
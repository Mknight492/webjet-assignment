/**
 * Creates a data URL for a simple placeholder image with text
 * @param text Text to display on the placeholder
 * @param width Width of the placeholder
 * @param height Height of the placeholder
 * @returns Data URL string for the placeholder image
 */
export const createPlaceholderImage = (text: string = 'No Image', width: number = 300, height: number = 450): string => {
  // Create a canvas element
  const canvas = document.createElement('canvas');
  canvas.width = width;
  canvas.height = height;
  
  const ctx = canvas.getContext('2d');
  if (!ctx) return '';
  
  // Fill with light gray background
  ctx.fillStyle = '#f0f0f0';
  ctx.fillRect(0, 0, width, height);
  
  // Add text
  ctx.font = 'bold 16px Arial';
  ctx.fillStyle = '#666666';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';
  ctx.fillText(text, width / 2, height / 2);
  
  // Return as data URL
  return canvas.toDataURL('image/png');
};

/**
 * Default placeholder image as a data URL
 */
export const defaultPlaceholder = createPlaceholderImage(); 
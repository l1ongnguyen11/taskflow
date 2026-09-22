import React from 'react';

interface LogoProps {
  size?: number;
  showText?: boolean;
  textColor?: string;
  className?: string;
}

export const Logo: React.FC<LogoProps> = ({
  size = 36,
  showText = true,
  textColor,
  className = '',
}) => {
  const brandColor = '#007BFF';
  const effectiveTextColor = textColor || brandColor;

  return (
    <div
      className={`taskflow-logo-container ${className}`}
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: size * 0.25,
        cursor: 'pointer',
        userSelect: 'none',
      }}
    >
      {/* TaskFlow Emblem Mark (TF with diagonal cut & curved hook) */}
      <svg
        width={size}
        height={size}
        viewBox="0 0 350 250"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
        style={{ flexShrink: 0 }}
      >
        <g fill={brandColor}>
          {/* T top bar, stem, and bottom curved hook */}
          <path d="
            M 45 30 
            L 200 30 
            L 155 78 
            L 155 175 
            C 155 197, 142 213, 120 213 
            C 98 213, 85 197, 85 175 
            L 85 148 
            L 50 148 
            L 50 175 
            C 50 220, 80 248, 120 248 
            C 160 248, 190 220, 190 175 
            L 190 78 
            L 120 78 
            L 23 78 
            Z
          " />
          
          {/* F Top Bar (above diagonal slash) */}
          <path d="
            M 225 30 
            L 365 30 
            L 343 78 
            L 203 78 
            Z
          " />

          {/* F Middle Bar and Stem */}
          <path d="
            M 178 105 
            L 330 105 
            L 308 153 
            L 220 153 
            L 220 248 
            L 178 248 
            Z
          " />
        </g>
      </svg>

      {showText && (
        <span
          style={{
            fontSize: size * 0.72,
            fontWeight: 700,
            fontStyle: 'italic',
            letterSpacing: '-0.5px',
            color: effectiveTextColor,
            fontFamily: "'Inter', system-ui, -apple-system, sans-serif",
            lineHeight: 1,
          }}
        >
          TaskFlow
        </span>
      )}
    </div>
  );
};


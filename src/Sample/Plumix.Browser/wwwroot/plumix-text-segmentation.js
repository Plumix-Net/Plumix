// Flutter web's text fragmenter retains every segment, including whitespace and punctuation.
const segmenter = typeof Intl.Segmenter === "function"
  ? new Intl.Segmenter([], { granularity: "word" })
  : null;

export function getWordBoundaries(text) {
  if (segmenter === null) return null;
  const boundaries = Array.from(segmenter.segment(text), segment => segment.index);
  boundaries.push(text.length);
  return boundaries;
}

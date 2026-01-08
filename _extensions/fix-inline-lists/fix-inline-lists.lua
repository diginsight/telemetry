-- Fix inline lists that should be proper list structures
-- Detects paragraphs where lines start with "- " after line breaks
-- Converts them to proper <ul><li> structure

function Para(block)
  -- Check if paragraph contains multiple SoftBreak/LineBreak followed by "- " patterns
  -- Only apply transformation if we have at least 2 list items to avoid false positives
  local listMarkers = {}
  
  for i, inline in ipairs(block.content) do
    if (inline.t == "SoftBreak" or inline.t == "LineBreak") and i < #block.content then
      local next = block.content[i + 1]
      -- Check if next element is "- " (dash as Str followed by Space)
      if next and next.t == "Str" and next.text == "-" then
        -- Verify there's a Space after the dash
        if i + 2 <= #block.content and block.content[i + 2].t == "Space" then
          table.insert(listMarkers, i)
        end
      end
    end
  end
  
  -- Only apply transformation if we have at least 2 list items
  if #listMarkers < 2 then
    return block
  end
  
  local hasListPattern = true
  local listStartIndex = listMarkers[1]
  
  -- Split into header and list items
  local headerContent = {}
  local listItems = {}
  local currentItem = {}
  
  -- Collect header content (before first SoftBreak/LineBreak + "- ")
  for i = 1, listStartIndex - 1 do
    table.insert(headerContent, block.content[i])
  end
  
  -- Process list items
  local i = listStartIndex + 1
  while i <= #block.content do
    local inline = block.content[i]
    
    -- Check for start of list item: "- " at beginning (Str "-" + Space)
    -- But only if preceded by SoftBreak/LineBreak or at start
    local isListMarker = false
    if inline.t == "Str" and inline.text == "-" and 
       i + 1 <= #block.content and block.content[i + 1].t == "Space" then
      -- Check if this is at the start or preceded by a break
      if i == listStartIndex + 1 then
        -- First list item
        isListMarker = true
      elseif i > 1 then
        local prev = block.content[i - 1]
        if prev.t == "SoftBreak" or prev.t == "LineBreak" then
          isListMarker = true
        end
      end
    end
    
    if isListMarker then
      -- Save previous item if it exists
      if #currentItem > 0 then
        table.insert(listItems, pandoc.Plain(currentItem))
        currentItem = {}
      end
      -- Skip the "- " (dash and space)
      i = i + 2
    -- Check for line break that might precede next list item
    elseif inline.t == "SoftBreak" or inline.t == "LineBreak" then
      -- Check if next is a list item marker
      if i + 1 <= #block.content and block.content[i + 1].t == "Str" and 
         block.content[i + 1].text == "-" and
         i + 2 <= #block.content and block.content[i + 2].t == "Space" then
        -- This break separates list items, don't add to current item
        i = i + 1
      else
        -- Keep the break in the current item (for multi-line items)
        table.insert(currentItem, inline)
        i = i + 1
      end
    else
      -- Regular content within list item
      table.insert(currentItem, inline)
      i = i + 1
    end
  end
  
  -- Add last item
  if #currentItem > 0 then
    table.insert(listItems, pandoc.Plain(currentItem))
  end
  
  -- If we didn't create any list items, return the original block unchanged
  if #listItems == 0 then
    return block
  end
  
  -- Create new blocks to return
  local result = {}
  
  if #headerContent > 0 then
    table.insert(result, pandoc.Para(headerContent))
  end
  
  if #listItems > 0 then
    table.insert(result, pandoc.BulletList(listItems))
  end
  
  -- If result is empty (shouldn't happen but safety check), return original
  if #result == 0 then
    return block
  end
  
  return result
end
